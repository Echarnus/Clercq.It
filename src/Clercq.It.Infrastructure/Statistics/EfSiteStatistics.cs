using System.Security.Cryptography;
using System.Text;
using Clercq.It.Application.Features.Statistics;
using Clercq.It.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Clercq.It.Infrastructure.Statistics;

/// <summary>
/// Visitor statistics in PostgreSQL (issue #429, docs/statistics.md). Today's views are kept per
/// visitor hash; after the day they are rolled up into daily totals and the hashes and the salt
/// are deleted. Daily totals are kept <see cref="RetentionDays"/> days.
/// </summary>
public sealed class EfSiteStatistics(ApplicationDbContext db, StatisticsState state, TimeProvider clock) : ISiteStatistics
{
    public const int RetentionDays = 400;

    // Serialises the rollup across container instances (pg_advisory_xact_lock key).
    private const long RollupLockKey = 429_001;

    public async Task RecordPageViewAsync(string path, string ipAddress, string userAgent, CancellationToken cancellationToken)
    {
        var today = Today();
        await RollupIfDueAsync(today, cancellationToken);

        var salt = await GetSaltAsync(today, cancellationToken);
        var visitor = VisitorHash(salt, ipAddress, userAgent);

        await db.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO "StatsPageVisits" ("Day", "Path", "VisitorHash", "Views")
            VALUES ({today}, {path}, {visitor}, 1)
            ON CONFLICT ("Day", "Path", "VisitorHash") DO UPDATE SET "Views" = "StatsPageVisits"."Views" + 1
            """,
            cancellationToken);
    }

    public async Task<StatisticsData> GetAsync(DateOnly firstDay, DateOnly lastDay, CancellationToken cancellationToken)
    {
        var today = Today();
        await RollupAsync(today, cancellationToken);

        var days = await db.StatsDays.AsNoTracking()
            .Where(d => d.Day >= firstDay && d.Day <= lastDay)
            .Select(d => new DailyStatisticsDto(d.Day, d.Views, d.UniqueViews, d.Visitors))
            .ToListAsync(cancellationToken);

        var pages = await db.StatsDailyPages.AsNoTracking()
            .Where(p => p.Day >= firstDay && p.Day <= lastDay)
            .GroupBy(p => p.Path)
            .Select(g => new PageStatisticsDto(g.Key, g.Sum(p => p.Views), g.Sum(p => p.UniqueViews)))
            .ToListAsync(cancellationToken);

        if (today >= firstDay && today <= lastDay)
        {
            // Today is not rolled up yet: count it from the per-visitor rows.
            var visits = await db.StatsPageVisits.AsNoTracking()
                .Where(v => v.Day == today)
                .Select(v => new { v.Path, v.VisitorHash, v.Views })
                .ToListAsync(cancellationToken);

            if (visits.Count > 0)
            {
                days.Add(new DailyStatisticsDto(
                    today,
                    visits.Sum(v => (long)v.Views),
                    visits.Count,
                    visits.Select(v => Convert.ToHexString(v.VisitorHash)).Distinct().Count()));

                pages = pages
                    .Concat(visits.GroupBy(v => v.Path)
                        .Select(g => new PageStatisticsDto(g.Key, g.Sum(v => (long)v.Views), g.Count())))
                    .GroupBy(p => p.Path)
                    .Select(g => new PageStatisticsDto(g.Key, g.Sum(p => p.PageViews), g.Sum(p => p.UniquePageViews)))
                    .ToList();
            }
        }

        return new StatisticsData(days, pages);
    }

    /// <summary>
    /// Rolls every day before <paramref name="today"/> up into daily totals, deletes their visitor
    /// hashes and salts, and deletes daily totals older than <see cref="RetentionDays"/>.
    /// </summary>
    public async Task RollupAsync(DateOnly today, CancellationToken cancellationToken)
    {
        var cutoff = today.AddDays(-RetentionDays);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock({RollupLockKey})", cancellationToken);

        await db.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO "StatsDailyPages" ("Day", "Path", "Views", "UniqueViews")
            SELECT "Day", "Path", SUM("Views"), COUNT(*) FROM "StatsPageVisits"
            WHERE "Day" < {today}
            GROUP BY "Day", "Path"
            ON CONFLICT ("Day", "Path") DO UPDATE SET
                "Views" = "StatsDailyPages"."Views" + EXCLUDED."Views",
                "UniqueViews" = "StatsDailyPages"."UniqueViews" + EXCLUDED."UniqueViews"
            """,
            cancellationToken);

        await db.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO "StatsDays" ("Day", "Views", "UniqueViews", "Visitors")
            SELECT "Day", SUM("Views"), COUNT(*), COUNT(DISTINCT "VisitorHash") FROM "StatsPageVisits"
            WHERE "Day" < {today}
            GROUP BY "Day"
            ON CONFLICT ("Day") DO UPDATE SET
                "Views" = "StatsDays"."Views" + EXCLUDED."Views",
                "UniqueViews" = "StatsDays"."UniqueViews" + EXCLUDED."UniqueViews",
                "Visitors" = "StatsDays"."Visitors" + EXCLUDED."Visitors"
            """,
            cancellationToken);

        await db.StatsPageVisits.Where(v => v.Day < today).ExecuteDeleteAsync(cancellationToken);
        await db.StatsSalts.Where(s => s.Day < today).ExecuteDeleteAsync(cancellationToken);
        await db.StatsDailyPages.Where(p => p.Day < cutoff).ExecuteDeleteAsync(cancellationToken);
        await db.StatsDays.Where(d => d.Day < cutoff).ExecuteDeleteAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        state.RememberRollup(today);
    }

    public static byte[] VisitorHash(byte[] salt, string ipAddress, string userAgent)
    {
        var input = Encoding.UTF8.GetBytes($"{ipAddress}\n{userAgent}");
        return SHA256.HashData([.. salt, .. input]);
    }

    private async Task RollupIfDueAsync(DateOnly today, CancellationToken cancellationToken)
    {
        if (!state.IsRolledUpBefore(today))
        {
            await RollupAsync(today, cancellationToken);
        }
    }

    private async Task<byte[]> GetSaltAsync(DateOnly today, CancellationToken cancellationToken)
    {
        if (state.SaltFor(today) is { } cached)
        {
            return cached;
        }

        // Several instances may start the day at once: the first insert wins, everyone reads it back.
        var candidate = RandomNumberGenerator.GetBytes(32);
        await db.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO "StatsSalts" ("Day", "Salt") VALUES ({today}, {candidate})
            ON CONFLICT ("Day") DO NOTHING
            """,
            cancellationToken);
        var salt = await db.StatsSalts.AsNoTracking()
            .Where(s => s.Day == today)
            .Select(s => s.Salt)
            .SingleAsync(cancellationToken);

        state.RememberSalt(today, salt);
        return salt;
    }

    private DateOnly Today() => DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
}
