using System.Globalization;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Clercq.It.Application.Features.MyZen;

namespace Clercq.It.Infrastructure.MyZen;

/// <summary>One page of an invoice list; <see cref="HasNextPage"/> when the pager links a further page.</summary>
public sealed record MyZenInvoiceListPage(IReadOnlyList<MyZenInvoiceSummaryDto> Invoices, bool HasNextPage);

/// <summary>
/// Reads MyZen's invoice and credit-note pages (issue #494): the lists under
/// <c>/Order/{Income|Cost}/{Invoice|CreditNote}/Index</c> and one document's
/// <c>EditOrder</c> page. Sent sales invoices render read-only (a label and plain
/// text), purchase invoices render as a form; the details parser reads both.
/// </summary>
public static partial class MyZenInvoiceHtml
{
    private const string ReadDateFormat = "d/M/yyyy";
    private const string StatusClassPrefix = "statusType";

    private static readonly HtmlParser Parser = new();

    public static MyZenInvoiceListPage ParseInvoiceList(string html, string direction, string kind, int page)
    {
        var document = Parser.ParseDocument(html);
        var table = document.QuerySelectorAll("table").FirstOrDefault(t => t.QuerySelector("thead th[sortcolumn]") is not null);
        if (table is null)
        {
            return new MyZenInvoiceListPage([], false);
        }

        // Columns are found by MyZen's sort keys: the purchase list has an extra category
        // column, and a credit note list names its invoice under a second OrderNumber.
        var columns = new Dictionary<string, int>(StringComparer.Ordinal);
        var headers = table.QuerySelectorAll("thead th").ToList();
        for (var i = 0; i < headers.Count; i++)
        {
            if (headers[i].GetAttribute("sortcolumn") is { Length: > 0 } key)
            {
                columns.TryAdd(key, i);
            }
        }

        var invoices = new List<MyZenInvoiceSummaryDto>();
        foreach (var row in table.QuerySelectorAll("tbody tr[navigateurl]"))
        {
            if (ParseOrderId(row.GetAttribute("navigateurl")) is not { } id)
            {
                continue;
            }

            var cells = row.Children.Where(c => c.LocalName == "td").ToList();
            IElement? Cell(string key) => columns.TryGetValue(key, out var i) && i < cells.Count ? cells[i] : null;
            string Text(string key) => Clean(Cell(key)?.TextContent);
            string? Optional(string key) => Text(key) is { Length: > 0 } text ? text : null;

            invoices.Add(new MyZenInvoiceSummaryDto(
                id,
                direction,
                kind,
                Text("OrderNumber"),
                ParseDate(Text("OrderDate")),
                ParseDate(Text("ExpiryDate")),
                Optional("Reference"),
                Optional("Title"),
                Text("CustomerName"),
                ParseAmount(Text("TotalPriceExcl")),
                ParseAmount(Text("TotalPriceVAT")),
                ParseAmount(Text("TotalPriceIncl")),
                ParseAmount(Text("ToPay")),
                StatusOf(Cell("OrderStatusTC")?.QuerySelector($"[class*={StatusClassPrefix}]")),
                row.QuerySelector("i.fa-file-circle-info")?.GetAttribute("title") is { Length: > 0 } category ? Clean(category) : null));
        }

        var next = (page + 1).ToString(CultureInfo.InvariantCulture);
        var hasNext = document.QuerySelectorAll(".pagination [data-page]").Any(a => a.GetAttribute("data-page") == next);
        return new MyZenInvoiceListPage(invoices, hasNext);
    }

    /// <summary>Reads one invoice or credit note from its page.</summary>
    /// <returns>The document, or null when the page holds no document with this ID.</returns>
    public static MyZenInvoiceDto? ParseInvoiceDetails(string html, string direction, string kind, long id)
    {
        var document = Parser.ParseDocument(html);
        if (Input(document, "Order.OrderID") is not { } idText || ParseId(idText) != id)
        {
            return null;
        }

        // A form field when the page is editable, otherwise the plain text next to its label.
        string Field(string name, string labelFor) =>
            Input(document, name) is { Length: > 0 } value ? Clean(value) : Clean(LabelledText(document, labelFor));
        string? Optional(string name, string labelFor) => Field(name, labelFor) is { Length: > 0 } text ? text : null;

        var category = Input(document, "AccountingCostCategoryName");

        return new MyZenInvoiceDto(
            id,
            direction,
            kind,
            Field("Order.OrderNumber", "Order_OrderNumber"),
            ParseDate(Field("Order.OrderDate", "Order_OrderDate")),
            ParseDate(Field("Order.ExpiryDate", "Order_ExpiryDate")),
            ParseDate(Field("Order.DeliveryDate", "Order_DeliveryDate")),
            Optional("Order.Reference", "Order_Reference"),
            Optional("Order.OrderTitle", "Order_OrderTitle"),
            Field("CustomerName", "CustomerName"),
            ParseId(Input(document, "CustomerID")),
            Input(document, "ProjectName") is { Length: > 0 } project ? Clean(project) : null,
            Input(document, "Order.CurrencyTC") is { Length: > 0 } currency ? currency : "EUR",
            ParseAmount(document.QuerySelector(".totalexcl")?.TextContent),
            ParseAmount(document.QuerySelector(".totalvat")?.TextContent),
            ParseAmount(document.QuerySelector(".totaltotal")?.TextContent),
            StatusOf(document.QuerySelector($"span.badge[class*={StatusClassPrefix}]")),
            Paid: document.QuerySelector("input[type=checkbox][name='Order.Paid']")?.HasAttribute("checked") == true,
            ParseDate(Input(document, "Order.PaidDate")),
            string.IsNullOrWhiteSpace(category) ? null : Clean(category),
            ParseLines(document));
    }

    private static List<MyZenInvoiceLineDto> ParseLines(IDocument document)
    {
        var lines = new List<MyZenInvoiceLineDto>();
        foreach (var row in document.QuerySelectorAll("tr").Where(r => r.QuerySelector("input[name$='.OrderLineID']") is not null))
        {
            string? Line(string field) =>
                row.QuerySelector($"input[name$='].{field}'], textarea[name$='].{field}']") is { } e
                    ? (e.LocalName == "textarea" ? e.TextContent : e.GetAttribute("value"))
                    : null;

            if (string.Equals(Line("Deleted"), "True", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var description = Line("Description") ?? row.QuerySelector("td.whiteSpaceNormal")?.TextContent;
            lines.Add(new MyZenInvoiceLineDto(
                Clean(description),
                ParseAmount(Line("Quantity")),
                Line("Unit") is { Length: > 0 } unit ? Clean(unit) : null,
                ParseAmount(Line("AmountExcl")),
                ParseAmount(Line("VAT")),
                Line("ReductionPercentage") is { Length: > 0 } discount ? ParseAmount(discount) : null,
                ParseAmount(Line("TotalExcl")),
                ParseAmount(Line("TotalVAT")),
                ParseAmount(Line("TotalIncl"))));
        }
        return lines;
    }

    /// <summary>The value of the first field with this name: a checked radio, a selected option, or a value.</summary>
    private static string? Input(IDocument document, string name)
    {
        var fields = document.QuerySelectorAll($"[name='{name}']").ToList();
        if (fields.Count == 0)
        {
            return null;
        }

        var radios = fields.Where(f => f.GetAttribute("type") == "radio").ToList();
        if (radios.Count > 0)
        {
            return radios.FirstOrDefault(r => r.HasAttribute("checked"))?.GetAttribute("value");
        }

        var field = fields.FirstOrDefault(f => f.GetAttribute("type") != "checkbox") ?? fields[0];
        return field.LocalName switch
        {
            "textarea" => field.TextContent,
            "select" => field.QuerySelector("option[selected]")?.GetAttribute("value") ?? string.Empty,
            _ => field.GetAttribute("value"),
        };
    }

    /// <summary>The text in the column next to a field's label, as read-only pages show it.</summary>
    private static string? LabelledText(IDocument document, string labelFor) =>
        document.QuerySelector($"label[for='{labelFor}']")?.ParentElement?.NextElementSibling?.TextContent;

    /// <summary>MyZen marks a status with a <c>statusType{Status}</c> class, e.g. <c>statusTypeToPay</c>.</summary>
    private static string StatusOf(IElement? element) =>
        element?.ClassList.FirstOrDefault(c => c.StartsWith(StatusClassPrefix, StringComparison.Ordinal) && c.Length > StatusClassPrefix.Length)
            ?[StatusClassPrefix.Length..]
        ?? string.Empty;

    /// <summary>Reads Belgian amounts such as <c>€ 12.967,5000</c> or <c>-10,74380</c>.</summary>
    public static decimal ParseAmount(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0m;
        }

        var negative = text.Contains('-', StringComparison.Ordinal);
        var digits = NotAmount().Replace(text, string.Empty).Replace(".", string.Empty, StringComparison.Ordinal)
            .Replace(',', '.');
        return decimal.TryParse(digits, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var amount)
            ? (negative ? -amount : amount)
            : 0m;
    }

    private static long? ParseOrderId(string? url) =>
        url is null ? null : OrderUrl().Match(url) is { Success: true } match ? ParseId(match.Groups[1].Value) : null;

    private static DateOnly? ParseDate(string? text)
    {
        var day = text?.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return DateOnly.TryParseExact(day, ReadDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;
    }

    private static long? ParseId(string? text) =>
        long.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var id) && id > 0 ? id : null;

    private static string Clean(string? text) =>
        string.Join(' ', (text ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    [GeneratedRegex(@"[^0-9.,]")]
    private static partial Regex NotAmount();

    [GeneratedRegex(@"/(\d+)/EditOrder", RegexOptions.IgnoreCase)]
    private static partial Regex OrderUrl();
}
