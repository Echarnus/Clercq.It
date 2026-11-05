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
    /// <summary>The format the gateway writes dates in; MyZen accepts it in filters and forms.</summary>
    public const string DateFormat = "dd/MM/yyyy";

    /// <summary>
    /// MyZen renders dates without leading zeros (<c>9/10/2026</c>, issue #489);
    /// <c>d</c> and <c>M</c> also read padded values.
    /// </summary>
    private const string ReadDateFormat = "d/M/yyyy";

    private static readonly HtmlParser Parser = new();
    private static readonly string[] ErrorProperties = ["Message", "Error", "ErrorMessage"];

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

    /// <summary>
    /// The anti-forgery token for MyZen's AJAX calls (the <c>RequestVerificationToken</c> header).
    /// Signed-in pages carry it as <c>rawcsrfToken</c> in <c>script#layoutmainconfig</c>; their
    /// hidden token inputs are only added by MyZen's scripts in the browser (issue #448).
    /// A hidden input in the served HTML is the fallback.
    /// </summary>
    public static string? ParseRequestVerificationToken(string html)
    {
        var document = Parser.ParseDocument(html);
        var config = document.QuerySelector("script#layoutmainconfig")?.TextContent;
        if (!string.IsNullOrWhiteSpace(config))
        {
            try
            {
                using var json = JsonDocument.Parse(config);
                if (json.RootElement.ValueKind == JsonValueKind.Object
                    && json.RootElement.TryGetProperty("rawcsrfToken", out var raw)
                    && raw.ValueKind == JsonValueKind.String
                    && raw.GetString() is { Length: > 0 } token)
                {
                    return token;
                }
            }
            catch (JsonException)
            {
                // Fall back to a hidden input below.
            }
        }

        return document.QuerySelector("input[name=__RequestVerificationToken]")?.GetAttribute("value") is { Length: > 0 } value
            ? value
            : null;
    }

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
            if (!TryParseDate(dateText, out var date))
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

    /// <summary>Reads one time entry from its edit form.</summary>
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
        if (!TryParseDate(dateText, out var date))
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

            // The list has no customer IDs; the gateway adds them from the customer list.
            projects.Add(new MyZenProjectDto(id, Cell(0), CustomerId: null, Cell(1), Optional(2), Optional(3), Cell(5)));
        }

        return (projects, HasPage(document, page + 1));
    }

    /// <summary>
    /// Reads MyZen's project lookup: <c>[{ value: id, label }]</c>, where the label is
    /// <c>Name[~]...[~]CustomerID[~]CustomerName</c>. Projects without a customer (Feestdag,
    /// Vakantie, Ziekte) carry MyZen's unfilled placeholders <c>[extradata-CustomerID]</c> and
    /// <c>[extradata-CustomerName]</c>; those become null.
    /// </summary>
    public static IReadOnlyList<MyZenProjectOptionDto> ParseProjectSuggestions(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var options = new List<MyZenProjectOptionDto>();
            foreach (var item in document.RootElement.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object
                    || !item.TryGetProperty("value", out var value)
                    || ParseId(value.ValueKind == JsonValueKind.Number ? value.GetRawText() : value.GetString()) is not { } id)
                {
                    continue;
                }

                var label = item.TryGetProperty("label", out var l) && l.ValueKind == JsonValueKind.String ? l.GetString()! : string.Empty;
                var parts = label.Split("[~]").Select(p => IsPlaceholder(p) ? string.Empty : p).ToArray();
                var customerId = parts.Skip(1).Select(ParseId).FirstOrDefault(p => p is not null);
                var customer = parts.Length > 2 && ParseId(parts[^1]) is null ? Clean(parts[^1]) : string.Empty;
                options.Add(new MyZenProjectOptionDto(id, Clean(parts[0]), customerId, customer.Length > 0 ? customer : null));
            }
            return options;
        }
        catch (JsonException)
        {
            return [];
        }
    }

    /// <summary>
    /// Reads one page of MyZen's customer list (<c>/Company/Customer</c>). Columns: select, created,
    /// number, name, VAT, contact, city, e-mail, phone, mobile, group, Peppol, actions. The ID is in
    /// the row's <c>navigateurl</c> (<c>/Company/Customer/{id}/CompanyEdit</c>).
    /// </summary>
    public static (IReadOnlyList<MyZenCustomerSummaryDto> Customers, bool HasNextPage) ParseCustomerList(string html, int page)
    {
        var document = Parser.ParseDocument(html);
        var customers = new List<MyZenCustomerSummaryDto>();

        foreach (var row in document.QuerySelectorAll("tbody tr[navigateurl]"))
        {
            if (CustomerIdFromUrl(row.GetAttribute("navigateurl")) is not { } id)
            {
                continue;
            }

            var cells = row.QuerySelectorAll("td").Select(td => Clean(td.TextContent)).ToList();
            string? Cell(int i) => i < cells.Count && cells[i].Length > 0 ? cells[i] : null;

            customers.Add(new MyZenCustomerSummaryDto(id, Cell(2), Cell(3) ?? string.Empty, Cell(4), Cell(5), Cell(6), Cell(7), Cell(8)));
        }

        return (customers, HasPage(document, page + 1));
    }

    /// <summary>Reads one customer from its edit page (<c>/Company/Customer/{id}/CompanyEdit</c>).</summary>
    /// <returns>The customer, or null when the page has no customer form.</returns>
    public static MyZenCustomerDto? ParseCustomerDetails(string html)
    {
        var form = Parser.ParseDocument(html).QuerySelector("form#pageform");
        if (form is null || ParseId(FieldValue(form, "Company.CompanyID")) is not { } id)
        {
            return null;
        }

        string? Field(string name) => FieldValue(form, name) is { Length: > 0 } value ? value.Trim() : null;

        return new MyZenCustomerDto(
            id,
            Field("Company.Nr"),
            Field("Company.CompanyName") ?? string.Empty,
            Field("Company.CommercialName"),
            form.QuerySelector("input[type=radio][name=CustomerTypeTC][checked]")?.GetAttribute("value"),
            Field("Company.VAT"),
            form.QuerySelector("input[type=checkbox][name='Company.VATLiable']")?.HasAttribute("checked") == true,
            Field("Company.Email"),
            Field("Company.Phone"),
            Field("Company.Mobile"),
            Field("Company.Website"),
            Field("Company.LanguageTC"),
            Field("Company.Reference"),
            Field("Company.ContactFirstName"),
            Field("Company.ContactLastName"),
            new MyZenAddressDto(
                Field("InvoiceAddress.Street"),
                Field("InvoiceAddress.StreetNumber"),
                Field("InvoiceAddress.Box"),
                Field("InvoiceAddress.Zipcode"),
                Field("InvoiceAddress.City"),
                Field("InvoiceAddress.Country")));
    }

    /// <summary>Reads one project from its edit page (<c>/Project/{id}/EditProject</c>).</summary>
    /// <returns>The project, or null when the page has no project form.</returns>
    public static MyZenProjectDetailsDto? ParseProjectDetails(string html)
    {
        var form = Parser.ParseDocument(html).QuerySelector("form#pageform");
        if (form is null || ParseId(FieldValue(form, "ProjectID")) is not { } id)
        {
            return null;
        }

        string? Field(string name) => FieldValue(form, name) is { Length: > 0 } value ? value.Trim() : null;
        DateOnly? Date(string name) =>
            TryParseDate(Field(name)?.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(), out var date) ? date : null;

        var status = form.QuerySelector("input[type=radio][name=ProjectStatus][checked]")?.GetAttribute("value") ?? string.Empty;
        var timeBudgetSeconds = ParseDecimal(Field("TimeBudget"));

        return new MyZenProjectDetailsDto(
            id,
            Field("ProjectName") ?? string.Empty,
            ParseId(Field("CustomerID")),
            Field("CustomerName"),
            MyZenProjectStatus.ToLabel(status),
            Date("StartDate"),
            Date("EndDate"),
            Field("Reference"),
            Field("Notes"),
            ParseDecimal(Field("Budget")),
            timeBudgetSeconds is > 0 ? Math.Round(timeBudgetSeconds.Value / 3600m, 2) : null,
            ParseDecimal(Field("Rate")));
    }

    private static long? CustomerIdFromUrl(string? url)
    {
        var parts = (url ?? string.Empty).Split('?')[0].Split('/', StringSplitOptions.RemoveEmptyEntries);
        var index = Array.FindIndex(parts, p => p.Equals("Customer", StringComparison.OrdinalIgnoreCase));
        return index >= 0 && index + 1 < parts.Length ? ParseId(parts[index + 1]) : null;
    }

    /// <summary>The first non-checkbox field with this name in the form.</summary>
    private static string? FieldValue(IElement form, string name) =>
        form.QuerySelector($"[name='{name}']:not([type=checkbox]):not([type=radio])") is { } e ? ValueOf(e) : null;

    /// <summary>MyZen writes amounts the Belgian way (<c>1.000,50</c>); plain <c>1000.50</c> is read too.</summary>
    private static decimal? ParseDecimal(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var normalised = text.Contains(',', StringComparison.Ordinal)
            ? text.Replace(".", string.Empty, StringComparison.Ordinal).Replace(',', '.')
            : text;
        return decimal.TryParse(normalised, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) ? value : null;
    }

    /// <summary>MyZen's unfilled template fields, such as <c>[extradata-CustomerName]</c>.</summary>
    private static bool IsPlaceholder(string text) =>
        text.Trim() is { Length: > 0 } t && t.StartsWith("[extradata-", StringComparison.OrdinalIgnoreCase) && t.EndsWith(']');

    /// <summary>
    /// Reads the page MyZen returns after an edit form is posted (customers and projects, issue #495).
    /// A saved form shows <c>.alert-success</c> ("De aanpassingen zijn succesvol opgeslagen.");
    /// a refused one shows ASP.NET validation messages. The red "Ook op deze pagina" badge
    /// (someone else viewing the page) is not an error.
    /// </summary>
    public static (bool Saved, string? Error) ParseFormSave(string html)
    {
        var document = Parser.ParseDocument(html);
        var errors = document
            .QuerySelectorAll(".field-validation-error, .validation-summary-errors li")
            .Select(e => Clean(e.TextContent))
            .Where(text => text.Length > 0)
            .Distinct()
            .ToList();
        if (errors.Count > 0)
        {
            return (false, Truncate(string.Join(" ", errors), 300));
        }

        return (document.QuerySelector(".alert-success:not(.d-none)") is not null, null);
    }

    /// <summary>Reads MyZen's answer to a save or delete.</summary>
    /// <returns>MyZen's reason when it refused the change, or null when it accepted it.</returns>
    public static string? ParseChangeError(string body)
    {
        var trimmed = body.TrimStart();
        if (trimmed.StartsWith('{'))
        {
            try
            {
                using var json = JsonDocument.Parse(trimmed);
                var root = json.RootElement;
                if (root.TryGetProperty("Success", out var success) && success.ValueKind == JsonValueKind.False)
                {
                    var message = ErrorProperties
                        .Select(name => root.TryGetProperty(name, out var m) && m.ValueKind == JsonValueKind.String ? m.GetString() : null)
                        .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m));
                    return Truncate(Clean(message ?? "MyZen refused the change."), 300);
                }
                return null;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        var error = Parser.ParseDocument(body)
            .QuerySelectorAll(".field-validation-error, .validation-summary-errors li, .alert-danger:not(.d-none)")
            .Select(e => Clean(e.TextContent))
            .Where(text => text.Length > 0)
            .ToList();
        return error.Count == 0 ? null : Truncate(string.Join(" ", error.Distinct()), 300);
    }

    private static bool HasPage(IDocument document, int page) =>
        document.QuerySelectorAll(".pagination [data-page]")
            .Any(a => a.GetAttribute("data-page") == page.ToString(CultureInfo.InvariantCulture));

    private static bool TryParseDate(string? text, out DateOnly date) =>
        DateOnly.TryParseExact(text, ReadDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

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
