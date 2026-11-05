using System.ComponentModel;
using Clercq.It.Application.Features.MyZen;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Server;

namespace Clercq.It.Api.Features.Mcp;

/// <summary>
/// The owner's MyZen sales and purchase invoices as read-only MCP tools (issue #494,
/// docs/myzen.md). Same access as <see cref="MyZenTools"/>: <c>MyZen.Owner</c>, and
/// hidden while the <c>MyZen</c> flag is off.
/// </summary>
[McpServerToolType]
[Authorize(Policy = MyZenTools.Policy)]
public sealed class MyZenInvoiceTools(McpContent content)
{
    [McpServerTool(Name = "list_myzen_invoices", ReadOnly = true)]
    [Description("Lists the company's invoices from MyZen in a period, oldest first. direction 'sales' gives the invoices it sent to customers (income); 'purchase' gives the invoices it received from suppliers (costs, with the cost category). Amounts are in EUR: totalExcl, vat and totalIncl, plus toPay (still open). Use kind 'creditNote' for credit notes, which reduce income or costs. The period is at most 366 days; call it once per year for longer spans. Use get_myzen_invoice for the lines of one invoice.")]
    public async Task<IReadOnlyList<MyZenInvoiceSummaryDto>> ListInvoicesAsync(
        [Description("'sales' (sent, income) or 'purchase' (received, costs)")] string direction,
        [Description("First invoice date, yyyy-MM-dd")] DateOnly from,
        [Description("Last invoice date, yyyy-MM-dd")] DateOnly to,
        [Description("'invoice' (default) or 'creditNote'")] string kind = MyZenInvoiceKind.Invoice,
        [Description("MyZen's own keyword search (number, reference, customer or supplier), max 100 characters")] string? search = null,
        [Description("Only this status: Paid, ToPay, Draft, ToSend, Credited, PaymentFileGenerated or ToDomiciliate")] string? status = null,
        [Description("Only invoices past their due date that are still open")] bool overdueOnly = false,
        [Description("Only invoices whose customer or supplier name contains this text, e.g. 'Anthropic'")] string? counterparty = null,
        [Description("Only invoices with a total incl. VAT of at least this amount")] decimal? minAmount = null,
        [Description("Only invoices with a total incl. VAT of at most this amount")] decimal? maxAmount = null,
        CancellationToken cancellationToken = default) =>
        await content.SendAsync(new GetMyZenInvoicesQuery(new MyZenInvoiceFilter(
            direction, kind, from, to, search, status, overdueOnly, counterparty, minAmount, maxAmount)), cancellationToken);

    [McpServerTool(Name = "get_myzen_invoice", ReadOnly = true)]
    [Description("Gets one MyZen invoice or credit note by the id, direction and kind list_myzen_invoices returned: dates, customer or supplier, project, currency, totals, status, whether and when it was paid, the cost category, and every line with quantity, unit price excl. VAT, VAT % and totals.")]
    public async Task<MyZenInvoiceDto> GetInvoiceAsync(
        [Description("'sales' or 'purchase', as listed")] string direction,
        [Description("The invoice id from list_myzen_invoices (not the invoice number)")] long id,
        [Description("'invoice' (default) or 'creditNote'")] string kind = MyZenInvoiceKind.Invoice,
        CancellationToken cancellationToken = default) =>
        await content.SendAsync(new GetMyZenInvoiceQuery(direction, kind, id), cancellationToken)
            ?? throw McpContent.NotFound($"MyZen {direction} {kind}", id);
}
