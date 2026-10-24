namespace Clercq.It.Application.Features.Statistics;

/// <summary>
/// First-party, cookieless visitor statistics kept in the site's own database
/// (issue #429, docs/statistics.md). A visitor is a hash of a random daily salt,
/// the IP address and the user agent; the IP address and user agent are never stored.
/// </summary>
public interface ISiteStatistics
{
    /// <summary>Counts one view of <paramref name="path"/> today (UTC) for the visitor behind the IP and user agent.</summary>
    Task RecordPageViewAsync(string path, string ipAddress, string userAgent, CancellationToken cancellationToken);

    /// <summary>The days and pages with views from <paramref name="firstDay"/> to <paramref name="lastDay"/>, both included.</summary>
    Task<StatisticsData> GetAsync(DateOnly firstDay, DateOnly lastDay, CancellationToken cancellationToken);
}

/// <summary>Raw counts as stored: only days and pages that had views.</summary>
public record StatisticsData(IReadOnlyList<DailyStatisticsDto> Days, IReadOnlyList<PageStatisticsDto> Pages);
