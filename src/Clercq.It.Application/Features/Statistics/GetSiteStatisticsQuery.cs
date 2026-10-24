using FluentValidation;
using MediatR;

namespace Clercq.It.Application.Features.Statistics;

/// <summary>Statistics for the last <paramref name="Days"/> days, today (UTC) included.</summary>
public record GetSiteStatisticsQuery(int Days) : IRequest<SiteStatisticsDto>;

public class GetSiteStatisticsQueryValidator : AbstractValidator<GetSiteStatisticsQuery>
{
    /// <summary>Daily totals are kept this long (docs/statistics.md#retention).</summary>
    public const int MaxDays = 400;

    public GetSiteStatisticsQueryValidator()
    {
        RuleFor(x => x.Days)
            .InclusiveBetween(1, MaxDays).WithMessage($"Days must be between 1 and {MaxDays}");
    }
}

public class GetSiteStatisticsQueryHandler(ISiteStatistics statistics, TimeProvider clock)
    : IRequestHandler<GetSiteStatisticsQuery, SiteStatisticsDto>
{
    public const int TopPageCount = 10;

    public async Task<SiteStatisticsDto> Handle(GetSiteStatisticsQuery request, CancellationToken cancellationToken)
    {
        var to = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var from = to.AddDays(1 - request.Days);
        var data = await statistics.GetAsync(from, to, cancellationToken);

        // One entry per day, so the trend shows quiet days as zero instead of skipping them.
        var byDate = data.Days.ToDictionary(d => d.Date);
        var days = Enumerable.Range(0, request.Days)
            .Select(offset => from.AddDays(offset))
            .Select(date => byDate.GetValueOrDefault(date) ?? new DailyStatisticsDto(date, 0, 0, 0))
            .ToList();

        var topPages = data.Pages
            .OrderByDescending(p => p.PageViews)
            .ThenBy(p => p.Path, StringComparer.Ordinal)
            .Take(TopPageCount)
            .ToList();

        return new SiteStatisticsDto(
            from,
            to,
            days.Sum(d => d.PageViews),
            days.Sum(d => d.UniquePageViews),
            days.Sum(d => d.Visitors),
            days,
            topPages);
    }
}
