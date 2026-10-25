using Clercq.It.Application.Features.Statistics;
using MediatR;

namespace Clercq.It.Api.Features;

/// <summary>
/// Visitor statistics (issue #429, docs/statistics.md): the site's page view beacon and the
/// admin statistics page's data.
/// </summary>
public static class StatisticsEndpoints
{
    public static IEndpointRouteBuilder MapStatisticsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/stats")
            .WithTags("Statistics");

        // No "api" rate limit policy: every page view calls this, the global limit still applies.
        group.MapPost("/pageview", async (PageViewRequest body, HttpContext context, IMediator mediator) =>
        {
            await mediator.Send(new RecordPageViewCommand(
                body.Path ?? string.Empty,
                ClientIp(context),
                context.Request.Headers.UserAgent.ToString()));
            return Results.NoContent();
        })
        .WithName("RecordPageView")
        .WithSummary("Count a page view")
        .WithDescription("Counts one view of a public page. No cookie is set and no IP address or user agent is stored.");

        group.MapGet("/", async (int? days, IMediator mediator) =>
            Results.Ok(await mediator.Send(new GetSiteStatisticsQuery(days ?? 30))))
        .RequireAuthorization("AdminView")
        .RequireRateLimiting("api")
        .WithName("GetSiteStatistics")
        .WithSummary("Get visitor statistics")
        .WithDescription("Page views, unique page views, unique visitors, top pages and a daily trend for the last `days` days (default 30). Requires the Admin.View role.");

        return endpoints;
    }

    /// <summary>
    /// The browser's address: the first <c>X-Forwarded-For</c> entry (Scaleway's edge, then nginx
    /// in the container, append to it), else the connection's address.
    /// </summary>
    public static string? ClientIp(HttpContext context)
    {
        var forwarded = context.Request.Headers["X-Forwarded-For"].ToString();
        var first = forwarded.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
        return first ?? context.Connection.RemoteIpAddress?.ToString();
    }

    public sealed record PageViewRequest(string? Path);
}
