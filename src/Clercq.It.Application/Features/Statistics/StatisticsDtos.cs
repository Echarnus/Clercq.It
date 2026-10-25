namespace Clercq.It.Application.Features.Statistics;

/// <summary>
/// Visitor statistics for a period (<c>GET /api/stats</c>). <c>Visitors</c> is the sum of each
/// day's unique visitors: the visitor hash changes every day, so someone who comes back on
/// another day counts again. <c>UniquePageViews</c> counts a page once per visitor per day.
/// </summary>
public record SiteStatisticsDto(
    DateOnly From,
    DateOnly To,
    long PageViews,
    long UniquePageViews,
    long Visitors,
    IReadOnlyList<DailyStatisticsDto> Days,
    IReadOnlyList<PageStatisticsDto> TopPages);

public record DailyStatisticsDto(DateOnly Date, long PageViews, long UniquePageViews, long Visitors);

public record PageStatisticsDto(string Path, long PageViews, long UniquePageViews);
