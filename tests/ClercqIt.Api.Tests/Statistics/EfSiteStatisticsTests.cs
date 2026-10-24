using Clercq.It.Infrastructure.Data;
using Clercq.It.Infrastructure.Statistics;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace ClercqIt.Api.Tests.Statistics;

/// <summary>The PostgreSQL statistics store: daily salts, rollup and retention.</summary>
[Collection(ApiTestGroup.Name)]
public class EfSiteStatisticsTests(CustomWebApplicationFactory factory) : IAsyncLifetime
{
    private const string Browser = "Mozilla/5.0 Firefox/131.0";
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 10, 20, 12, 0, 0, TimeSpan.Zero));
    private readonly StatisticsState _state = new();
    private IServiceScope _scope = null!;
    private ApplicationDbContext _db = null!;

    public async Task InitializeAsync()
    {
        await factory.ResetDatabaseAsync();
        _scope = factory.Services.CreateScope();
        _db = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    }

    public Task DisposeAsync()
    {
        _scope.Dispose();
        return Task.CompletedTask;
    }

    private EfSiteStatistics Store() => new(_db, _state, _clock);

    [Fact(DisplayName = "The next day's first page view rolls yesterday up and deletes its hashes and salt")]
    public async Task RecordPageView_NextDay_RollsUpAndForgetsVisitors()
    {
        // Arrange
        var store = Store();
        await store.RecordPageViewAsync("/", "203.0.113.7", Browser, CancellationToken.None);
        await store.RecordPageViewAsync("/", "203.0.113.7", Browser, CancellationToken.None);
        await store.RecordPageViewAsync("/blogs", "203.0.113.7", Browser, CancellationToken.None);
        await store.RecordPageViewAsync("/", "198.51.100.4", Browser, CancellationToken.None);

        // Act
        _clock.Advance(TimeSpan.FromDays(1));
        await store.RecordPageViewAsync("/", "203.0.113.7", Browser, CancellationToken.None);

        // Assert
        var yesterday = new DateOnly(2026, 10, 20);
        var day = await _db.StatsDays.AsNoTracking().SingleAsync();
        day.Should().BeEquivalentTo(new StatsDayRecord { Day = yesterday, Views = 4, UniqueViews = 3, Visitors = 2 });
        (await _db.StatsDailyPages.AsNoTracking().OrderBy(p => p.Path).ToListAsync()).Should().BeEquivalentTo(
        [
            new StatsDailyPageRecord { Day = yesterday, Path = "/", Views = 3, UniqueViews = 2 },
            new StatsDailyPageRecord { Day = yesterday, Path = "/blogs", Views = 1, UniqueViews = 1 },
        ]);
        (await _db.StatsPageVisits.AsNoTracking().ToListAsync()).Should().ContainSingle()
            .Which.Day.Should().Be(yesterday.AddDays(1));
        (await _db.StatsSalts.AsNoTracking().Select(s => s.Day).ToListAsync()).Should().Equal(yesterday.AddDays(1));
    }

    [Fact(DisplayName = "The same visitor gets a different hash on another day")]
    public async Task RecordPageView_TwoDays_HashChanges()
    {
        // Arrange
        var store = Store();
        await store.RecordPageViewAsync("/", "203.0.113.7", Browser, CancellationToken.None);
        var first = (await _db.StatsPageVisits.AsNoTracking().SingleAsync()).VisitorHash;

        // Act
        _clock.Advance(TimeSpan.FromDays(1));
        await store.RecordPageViewAsync("/", "203.0.113.7", Browser, CancellationToken.None);

        // Assert
        var second = (await _db.StatsPageVisits.AsNoTracking().SingleAsync()).VisitorHash;
        second.Should().HaveCount(32).And.NotEqual(first);
    }

    [Fact(DisplayName = "Statistics combine rolled-up days with today's live counts")]
    public async Task GetAsync_PastAndToday_ReturnsBoth()
    {
        // Arrange
        var store = Store();
        await store.RecordPageViewAsync("/", "203.0.113.7", Browser, CancellationToken.None);
        _clock.Advance(TimeSpan.FromDays(1));
        await store.RecordPageViewAsync("/", "203.0.113.7", Browser, CancellationToken.None);
        await store.RecordPageViewAsync("/", "198.51.100.4", Browser, CancellationToken.None);

        // Act
        var data = await store.GetAsync(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 21), CancellationToken.None);

        // Assert
        data.Days.Select(d => (d.Date, d.PageViews, d.Visitors)).Should().BeEquivalentTo(
        [
            (new DateOnly(2026, 10, 20), 1L, 1L),
            (new DateOnly(2026, 10, 21), 2L, 2L),
        ]);
        data.Pages.Should().ContainSingle().Which.PageViews.Should().Be(3);
    }

    [Fact(DisplayName = "The rollup deletes daily totals older than the retention period")]
    public async Task RollupAsync_OldTotals_AreDeleted()
    {
        // Arrange
        var today = new DateOnly(2026, 10, 20);
        var old = today.AddDays(-EfSiteStatistics.RetentionDays - 1);
        var kept = today.AddDays(-EfSiteStatistics.RetentionDays);
        _db.StatsDays.AddRange(new StatsDayRecord { Day = old, Views = 1 }, new StatsDayRecord { Day = kept, Views = 1 });
        _db.StatsDailyPages.AddRange(
            new StatsDailyPageRecord { Day = old, Path = "/", Views = 1 },
            new StatsDailyPageRecord { Day = kept, Path = "/", Views = 1 });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        // Act
        await Store().RollupAsync(today, CancellationToken.None);

        // Assert
        (await _db.StatsDays.AsNoTracking().Select(d => d.Day).ToListAsync()).Should().Equal(kept);
        (await _db.StatsDailyPages.AsNoTracking().Select(p => p.Day).ToListAsync()).Should().Equal(kept);
    }
}
