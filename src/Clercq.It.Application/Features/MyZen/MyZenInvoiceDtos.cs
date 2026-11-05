namespace Clercq.It.Application.Features.MyZen;

/// <summary>Which side of the books an invoice is on (issue #494).</summary>
public static class MyZenInvoiceDirection
{
    /// <summary>Invoices the company sent (MyZen "Inkomsten").</summary>
    public const string Sales = "sales";

    /// <summary>Invoices the company received (MyZen "Uitgaven").</summary>
    public const string Purchase = "purchase";

    public static readonly IReadOnlyList<string> All = [Sales, Purchase];
}

/// <summary>Whether a document is an invoice or a credit note.</summary>
public static class MyZenInvoiceKind
{
    public const string Invoice = "invoice";
    public const string CreditNote = "creditNote";

    public static readonly IReadOnlyList<string> All = [Invoice, CreditNote];
}

/// <summary>MyZen's own invoice statuses, as its list filter names them.</summary>
public static class MyZenInvoiceStatus
{
    public static readonly IReadOnlyList<string> All =
        ["Paid", "ToPay", "Draft", "ToSend", "Credited", "PaymentFileGenerated", "ToDomiciliate"];
}

/// <summary>
/// What to list. <c>Search</c> is MyZen's own keyword search; <c>Counterparty</c>,
/// <c>MinAmount</c> and <c>MaxAmount</c> (on the total incl. VAT) are applied to MyZen's results.
/// </summary>
public record MyZenInvoiceFilter(
    string Direction,
    string Kind,
    DateOnly From,
    DateOnly To,
    string? Search = null,
    string? Status = null,
    bool OverdueOnly = false,
    string? Counterparty = null,
    decimal? MinAmount = null,
    decimal? MaxAmount = null);

/// <summary>
/// One invoice or credit note as MyZen lists it. Amounts are in the invoice's currency
/// (EUR unless the details say otherwise). <c>Counterparty</c> is the customer of a sales
/// invoice or the supplier of a purchase invoice; <c>Category</c> is the cost category of a
/// purchase invoice. <c>Status</c> is one of <see cref="MyZenInvoiceStatus.All"/>.
/// </summary>
public record MyZenInvoiceSummaryDto(
    long Id,
    string Direction,
    string Kind,
    string Number,
    DateOnly? Date,
    DateOnly? DueDate,
    string? Reference,
    string? Subject,
    string Counterparty,
    decimal TotalExcl,
    decimal Vat,
    decimal TotalIncl,
    decimal ToPay,
    string Status,
    string? Category);

/// <summary>One invoice with its lines, read from its MyZen page.</summary>
public record MyZenInvoiceDto(
    long Id,
    string Direction,
    string Kind,
    string Number,
    DateOnly? Date,
    DateOnly? DueDate,
    DateOnly? DeliveryDate,
    string? Reference,
    string? Subject,
    string Counterparty,
    long? CounterpartyId,
    string? Project,
    string Currency,
    decimal TotalExcl,
    decimal Vat,
    decimal TotalIncl,
    string Status,
    bool Paid,
    DateOnly? PaidDate,
    string? Category,
    IReadOnlyList<MyZenInvoiceLineDto> Lines);

/// <summary>An invoice line. Prices exclude VAT unless named otherwise.</summary>
public record MyZenInvoiceLineDto(
    string Description,
    decimal Quantity,
    string? Unit,
    decimal UnitPrice,
    decimal VatPercentage,
    decimal? DiscountPercentage,
    decimal TotalExcl,
    decimal TotalVat,
    decimal TotalIncl);
