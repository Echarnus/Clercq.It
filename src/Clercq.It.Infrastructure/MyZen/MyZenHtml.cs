using System.Globalization;
using System.Text.Json;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Clercq.It.Application.Features.MyZen;

namespace Clercq.It.Infrastructure.MyZen;

/// <summary>
/// The parts of the MyZen login page a server-side sign-in needs. <c>CaptchaImage</c> is the
/// CAPTCHA as a data URL when the page shows one; <c>Error</c> is MyZen's own error text, if shown.
/// </summary>
public sealed record MyZenLoginPage(string? FormToken, string? HeaderToken, string? CaptchaCheck, string? CaptchaImage, string? Error);

/// <summary>One page of the time-entry list; <see cref="HasNextPage"/> when the pager links a further page.</summary>
public sealed record MyZenTimeEntryListPage(IReadOnlyList<MyZenTimeEntryDto> Entries, bool HasNextPage);

/// <summary>
/// Reads MyZen's server-rendered HTML (ASP.NET MVC with Bootstrap). The selectors
/// follow the pages as mapped in issue #345; when MyZen changes its markup, the
/// parsers return what they still find and the tests in MyZenHtmlTests show what moved.
/// </summary>
public static class MyZenHtml
{
    public const string DateFormat = "dd/MM/yyyy";

    private static readonly HtmlParser Parser = new();

    public static MyZenLoginPage ParseLoginPage(string html)
    {
        var document = Parser.ParseDocument(html);

        string? formToken = null, headerToken = null;
        var config = document.QuerySelector("script#LogonTokenConfig")?.TextContent;
        if (!string.IsNullOrWhiteSpace(config))
        {
            using var json = JsonDocument.Parse(config);
            if (json.RootElement.TryGetProperty("rawcsrfToken", out var raw))
            {
                headerToken = raw.GetString();
            }
            if (json.RootElement.TryGetProperty("csrfToken", out var input) && input.GetString() is { } inputHtml)
            {
                formToken = Parser.ParseDocument(inputHtml).QuerySelector("input")?.GetAttribute("value");
            }
        }
        formToken ??= document.QuerySelector("input[name=__RequestVerificationToken]")?.GetAttribute("value");

        var captcha = document.QuerySelector("div.captcha");
        var captchaImage = captcha is not null && !captcha.ClassList.Contains("d-none")
            ? captcha.QuerySelector("img[src^='data:image']")?.GetAttribute("src")
            : null;

        var error = document.QuerySelectorAll(".alert-danger")
            .Where(e => !e.ClassList.Contains("d-none"))
            .Select(e => Clean(e.TextContent))
            .FirstOrDefault(text => text.Length > 0);

        return new MyZenLoginPage(
            formToken,
            headerToken,
            document.QuerySelector("input[name=CaptchaCheck]")?.GetAttribute("value"),
            captchaImage,
            error is null ? null : Truncate(error, 300));
    }

    /// <summary>The anti-forgery token any signed-in page carries, for MyZen's AJAX calls.</summary>
    public static string? ParseRequestVerificationToken(string html) =>
        Parser.ParseDocument(html).QuerySelector("input[name=__RequestVerificationToken]")?.GetAttribute("value");

    /// <summary>The time-entry filter form's current values (it carries the owner's user ID).</summary>
    public static IReadOnlyList<KeyValuePair<string, string>> ParseTimeEntryFilter(string html)
    {
        var form = Parser.ParseDocument(html).QuerySelector("form#gridform");
        if (form is null)
        {
            return [];
        }

        return form.QuerySelectorAll("input[name], select[name]")
            .Select(e => KeyValuePair.Create(e.GetAttribute("name")!, ValueOf(e)))
            .ToList();
    }

    public static MyZenTimeEntryListPage ParseTimeEntryList(string html, int page)
    {
        var document = Parser.ParseDocument(html);
        var entries = new List<MyZenTimeEntryDto>();

        foreach (var row in document.QuerySelectorAll("tbody tr"))
        {
            var idText = row.QuerySelector("input[name$='.TimeRegistrationID']")?.GetAttribute("value");
            if (!long.TryParse(idText, NumberStyles.None, CultureInfo.InvariantCulture, out var id))
            {
                continue;
            }

            var summary = row.QuerySelector("td.d-md-table-cell[class*=maxw]");
            var dateText = Clean(summary?.QuerySelector(".font-weight-bold")?.TextContent);
            if (!DateOnly.TryParseExact(dateText, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                continue;
            }

            var columns = row.QuerySelectorAll("td.text-nowrap.d-md-table-cell").ToList();
            var durationText = row.QuerySelector("td.d-md-table-cell .timer")?.GetAttribute("data-duration");
            var seconds = long.TryParse(durationText, NumberStyles.None, CultureInfo.InvariantCulture, out var s) ? s : 0;

            entries.Add(new MyZenTimeEntryDto(
                id,
                date,
                Clean(summary?.QuerySelector(".text-truncate")?.TextContent),
                ProjectId: null,
                Project: columns.Count > 1 ? Clean(columns[1].TextContent) : string.Empty,
                CustomerId: null,
                Customer: columns.Count > 0 ? Clean(columns[0].TextContent) : string.Empty,
                TimeSpan.FromSeconds(seconds),
                Invoiced: columns.Count > 2 && columns[2].QuerySelector(".badge-success") is not null));
        }

        return new MyZenTimeEntryListPage(entries, HasPage(document, page + 1));
    }

    /// <returns>The entry in the edit form, or null when the form has no entry.</returns>
    public static MyZenTimeEntryDto? ParseTimeEntryDetails(string html)
    {
        var document = Parser.ParseDocument(html);
        string? Field(string name) =>
            document.QuerySelector($"[name='EditTimeRegistration.{name}']:not([type=checkbox])") is { } e ? ValueOf(e) : null;

        if (!long.TryParse(Field("TimeRegistrationID"), NumberStyles.None, CultureInfo.InvariantCulture, out var id))
        {
            return null;
        }

        var dateText = Field("Date")?.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (!DateOnly.TryParseExact(dateText, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return null;
        }

        return new MyZenTimeEntryDto(
            id,
            date,
            Field("Description") ?? string.Empty,
            ParseId(Field("ProjectID")),
            Field("ProjectName") ?? string.Empty,
            ParseId(Field("CustomerID")),
            Field("CustomerName") ?? string.Empty,
            ParseDuration(Field("Duration")),
            Invoiced: document.QuerySelector("input[type=checkbox][name='EditTimeRegistration.Invoiced']")?.HasAttribute("checked") == true);
    }

    public static (IReadOnlyList<MyZenProjectDto> Projects, bool HasNextPage) ParseProjectList(string html, int page)
    {
        var document = Parser.ParseDocument(html);
        var projects = new List<MyZenProjectDto>();

        foreach (var row in document.QuerySelectorAll("tbody tr[id^='ProjectID-']"))
        {
            if (ParseId(row.Id!["ProjectID-".Length..]) is not { } id)
            {
                continue;
            }

            var cells = row.QuerySelectorAll("td").Select(td => Clean(td.TextContent)).ToList();
            string Cell(int i) => i < cells.Count ? cells[i] : string.Empty;
            string? Optional(int i) => Cell(i) is { Length: > 0 } text ? text : null;

            projects.Add(new MyZenProjectDto(id, Cell(0), Cell(1), Optional(2), Optional(3), Cell(5)));
        }

        return (projects, HasPage(document, page + 1));
    }

    private static bool HasPage(IDocument document, int page) =>
        document.QuerySelectorAll(".pagination [data-page]")
            .Any(a => a.GetAttribute("data-page") == page.ToString(CultureInfo.InvariantCulture));

    private static long? ParseId(string? text) =>
        long.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var id) && id > 0 ? id : null;

    /// <summary>MyZen shows durations as <c>hh:mm</c> or <c>hh:mm:ss</c>.</summary>
    private static TimeSpan ParseDuration(string? text)
    {
        var parts = (text ?? string.Empty).Split(':');
        var numbers = parts.Select(p => int.TryParse(p, NumberStyles.None, CultureInfo.InvariantCulture, out var n) ? n : -1).ToList();
        if (parts.Length is < 2 or > 3 || numbers.Any(n => n < 0))
        {
            return TimeSpan.Zero;
        }

        return new TimeSpan(numbers[0], numbers[1], parts.Length == 3 ? numbers[2] : 0);
    }

    private static string ValueOf(IElement element) =>
        element.LocalName switch
        {
            "textarea" => element.TextContent,
            "select" => element.QuerySelector("option[selected]")?.GetAttribute("value") ?? string.Empty,
            _ => element.GetAttribute("value") ?? string.Empty,
        };

    private static string Clean(string? text) =>
        string.Join(' ', (text ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static string Truncate(string text, int length) => text.Length <= length ? text : text[..length];
}
