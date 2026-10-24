using System.Text.RegularExpressions;
using FluentValidation;
using MediatR;

namespace Clercq.It.Application.Features.Statistics;

/// <summary>
/// One page view from the site's beacon (<c>POST /api/stats/pageview</c>). Returns whether it
/// was counted: bots, admin pages and requests without an IP address or user agent are not.
/// </summary>
public record RecordPageViewCommand(string Path, string? IpAddress, string? UserAgent) : IRequest<bool>;

public partial class RecordPageViewCommandValidator : AbstractValidator<RecordPageViewCommand>
{
    public const int MaxPathLength = 200;

    public RecordPageViewCommandValidator()
    {
        // Only the path, as the browser's router reports it: no query string, no fragment.
        RuleFor(x => x.Path)
            .NotEmpty().WithMessage("Path is required")
            .MaximumLength(MaxPathLength).WithMessage($"Path must not exceed {MaxPathLength} characters")
            .Matches(PathPattern()).WithMessage("Path must start with / and contain only URL path characters");
    }

    [GeneratedRegex("^/[A-Za-z0-9._~%/-]*$")]
    private static partial Regex PathPattern();
}

public partial class RecordPageViewCommandHandler(ISiteStatistics statistics) : IRequestHandler<RecordPageViewCommand, bool>
{
    /// <summary>Paths that are not part of the public site.</summary>
    private static readonly string[] IgnoredPrefixes = ["/admin", "/api"];

    public async Task<bool> Handle(RecordPageViewCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.IpAddress) || string.IsNullOrWhiteSpace(request.UserAgent)
            || IsBot(request.UserAgent))
        {
            return false;
        }

        var path = NormalizePath(request.Path);
        if (IgnoredPrefixes.Any(prefix => path == prefix || path.StartsWith(prefix + "/", StringComparison.Ordinal)))
        {
            return false;
        }

        await statistics.RecordPageViewAsync(path, request.IpAddress.Trim(), request.UserAgent.Trim(), cancellationToken);
        return true;
    }

    /// <summary>Lower case, without a trailing slash, so <c>/Blogs/</c> and <c>/blogs</c> count as one page.</summary>
    public static string NormalizePath(string path)
    {
        var normalized = path.Trim().ToLowerInvariant().TrimEnd('/');
        return normalized.Length == 0 ? "/" : normalized;
    }

    /// <summary>Crawlers, link previews, monitors, headless browsers and HTTP libraries.</summary>
    public static bool IsBot(string userAgent) => BotPattern().IsMatch(userAgent);

    [GeneratedRegex(
        @"bot|crawl|spider|slurp|archiver|facebookexternalhit|embedly|preview|lighthouse|pagespeed|pingdom|uptime|monitor|headless|phantom|selenium|playwright|puppeteer|curl|wget|python|java/|go-http|okhttp|axios|node-fetch|undici|httpclient|libwww|scrapy",
        RegexOptions.IgnoreCase)]
    private static partial Regex BotPattern();
}
