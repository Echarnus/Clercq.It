using System.Globalization;
using Clercq.It.Application.Features.MyZen;

namespace Clercq.It.Infrastructure.MyZen;

/// <summary>
/// Read-only access to MyZen's sales and purchase invoices and credit notes (issue #494).
/// Only GETs of the pages MyZen's own browser UI shows; nothing is ever changed.
/// </summary>
public sealed partial class MyZenGateway : IMyZenInvoiceGateway
{
    public Task<IReadOnlyList<MyZenInvoiceSummaryDto>> GetInvoicesAsync(MyZenInvoiceFilter filter, CancellationToken cancellationToken) =>
        WithSession<IReadOnlyList<MyZenInvoiceSummaryDto>>(async session =>
        {
            var basePath = InvoicePath(filter.Direction, filter.Kind) + "/Index";
            var invoices = new List<MyZenInvoiceSummaryDto>();
            for (var page = 0; page < MaxPages; page++)
            {
                var response = await session.GetAsync(basePath + InvoiceQuery(filter, page), cancellationToken);
                if (response.IsLoginPage)
                {
                    return null;
                }

                var list = MyZenInvoiceHtml.ParseInvoiceList(response.Body, filter.Direction, filter.Kind, page);
                invoices.AddRange(list.Invoices);
                if (!list.HasNextPage)
                {
                    break;
                }
            }

            // MyZen lists newest first; read a period from its first day, like the time entries.
            return invoices.OrderBy(i => i.Date).ThenBy(i => i.Id).ToList();
        }, cancellationToken);

    public async Task<MyZenInvoiceDto?> GetInvoiceAsync(string direction, string kind, long id, CancellationToken cancellationToken)
    {
        var found = await WithSession<IReadOnlyList<MyZenInvoiceDto>>(async session =>
        {
            var response = await session.GetAsync(
                $"{InvoicePath(direction, kind)}/{id.ToString(CultureInfo.InvariantCulture)}/EditOrder", cancellationToken);
            if (response.IsLoginPage)
            {
                return null;
            }

            // Any other page (an error, another document) means MyZen has no such document here.
            return MyZenInvoiceHtml.ParseInvoiceDetails(response.Body, direction, kind, id) is { } invoice ? [invoice] : [];
        }, cancellationToken);
        return found.SingleOrDefault();
    }

    /// <summary>MyZen's area for a direction and kind, e.g. <c>/Order/Cost/Invoice</c>.</summary>
    private static string InvoicePath(string direction, string kind)
    {
        var side = direction switch
        {
            MyZenInvoiceDirection.Sales => "Income",
            MyZenInvoiceDirection.Purchase => "Cost",
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unknown invoice direction."),
        };
        var type = kind switch
        {
            MyZenInvoiceKind.Invoice => "Invoice",
            MyZenInvoiceKind.CreditNote => "CreditNote",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown invoice kind."),
        };
        return $"/Order/{side}/{type}";
    }

    /// <summary>The list's filter form fields, as MyZen's grid form sends them with GET.</summary>
    private static string InvoiceQuery(MyZenInvoiceFilter filter, int page)
    {
        var fields = new List<(string Key, string Value)>
        {
            ("F.DateFrom", filter.From.ToString(MyZenHtml.DateFormat, CultureInfo.InvariantCulture)),
            ("F.DateTill", filter.To.ToString(MyZenHtml.DateFormat, CultureInfo.InvariantCulture)),
        };
        if (filter.Search is { Length: > 0 } search)
        {
            fields.Add(("F.Keyword", search));
        }
        if (filter.Status is { Length: > 0 } status)
        {
            fields.Add(("F.OrderStatus", status));
        }
        if (filter.OverdueOnly)
        {
            fields.Add(("F.OverDue", "True"));
        }
        fields.Add(("F.CurrentPage", page.ToString(CultureInfo.InvariantCulture)));

        return "?" + string.Join('&', fields.Select(f => $"{Uri.EscapeDataString(f.Key)}={Uri.EscapeDataString(f.Value)}"));
    }
}
