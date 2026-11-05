namespace Clercq.It.Application.Features.MyZen;

/// <summary>
/// The owner's MyZen invoices, read-only (issue #494). Uses the same session as
/// <see cref="IMyZenGateway"/>; see docs/myzen.md.
/// </summary>
public interface IMyZenInvoiceGateway
{
    /// <summary>
    /// The invoices or credit notes MyZen lists for the filter's direction, kind, period,
    /// keyword, status and overdue flag, oldest first. Counterparty and amount filters are
    /// left to the caller.
    /// </summary>
    /// <exception cref="MyZenNotConnectedException">No live session.</exception>
    Task<IReadOnlyList<MyZenInvoiceSummaryDto>> GetInvoicesAsync(MyZenInvoiceFilter filter, CancellationToken cancellationToken);

    /// <summary>One invoice with its lines, or null when MyZen has none with this ID.</summary>
    /// <exception cref="MyZenNotConnectedException">No live session.</exception>
    Task<MyZenInvoiceDto?> GetInvoiceAsync(string direction, string kind, long id, CancellationToken cancellationToken);
}
