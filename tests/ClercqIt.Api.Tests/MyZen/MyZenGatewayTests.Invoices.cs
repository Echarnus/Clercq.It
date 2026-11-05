using Clercq.It.Application.Features.MyZen;
using FluentAssertions;

namespace ClercqIt.Api.Tests.MyZen;

/// <summary>The gateway's invoice reads (issue #494) against the scripted MyZen.</summary>
public partial class MyZenGatewayTests
{
    private static readonly MyZenInvoiceFilter SalesFilter =
        new("sales", "invoice", new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 31));

    [Fact(DisplayName = "Invoices are read with GETs, the period and every page, oldest first")]
    public async Task GetInvoices_ReadsAllPages()
    {
        // Arrange
        await ConnectAsync();
        _myZen.Respond(HttpMethod.Get, "/Order/Income/Invoice/Index", MyZenInvoicePages.SalesList(0, 1));
        _myZen.Respond(HttpMethod.Get, "/Order/Income/Invoice/Index", MyZenInvoicePages.SalesList(0, 1));

        // Act
        var invoices = await Gateway().GetInvoicesAsync(SalesFilter, CancellationToken.None);

        // Assert
        invoices.Should().HaveCount(4);
        invoices.Select(i => i.Date).Should().BeInAscendingOrder();
        _myZen.Requests.Should().OnlyContain(r => r.Method == "GET");
        _myZen.Requests.Select(r => r.Query).Should().Equal(
            "?F.DateFrom=01%2F01%2F2026&F.DateTill=31%2F03%2F2026&F.CurrentPage=0",
            "?F.DateFrom=01%2F01%2F2026&F.DateTill=31%2F03%2F2026&F.CurrentPage=1");
        _store.Record!.State.Should().Be(MyZenSessionState.Connected);
    }

    [Fact(DisplayName = "Purchase credit notes use MyZen's cost area and pass keyword, status and overdue")]
    public async Task GetInvoices_PurchaseCreditNotes_SendsFilters()
    {
        // Arrange
        await ConnectAsync();
        _myZen.Respond(HttpMethod.Get, "/Order/Cost/CreditNote/Index", MyZenInvoicePages.CreditNoteList);
        var filter = new MyZenInvoiceFilter("purchase", "creditNote", new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 31),
            Search: "Initech & co", Status: "ToPay", OverdueOnly: true);

        // Act
        var notes = await Gateway().GetInvoicesAsync(filter, CancellationToken.None);

        // Assert
        notes.Should().ContainSingle().Which.Number.Should().Be("CN-0007");
        _myZen.Requests.Single().Query.Should().Be(
            "?F.DateFrom=01%2F05%2F2026&F.DateTill=31%2F05%2F2026&F.Keyword=Initech%20%26%20co&F.OrderStatus=ToPay&F.OverDue=True&F.CurrentPage=0");
    }

    [Fact(DisplayName = "An expired session while listing invoices is marked expired and throws NotConnected")]
    public async Task GetInvoices_Expired_Throws()
    {
        // Arrange
        await ConnectAsync();
        _myZen.Respond(HttpMethod.Get, "/Order/Income/Invoice/Index", MyZenPages.Login(), finalPath: "/Account/Logon");

        // Act
        var act = () => Gateway().GetInvoicesAsync(SalesFilter, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<MyZenNotConnectedException>();
        _store.Record!.State.Should().Be(MyZenSessionState.Expired);
    }

    [Fact(DisplayName = "Invoices without a session throw NotConnected and call nothing")]
    public async Task GetInvoices_NoSession_Throws()
    {
        var act = () => Gateway().GetInvoicesAsync(SalesFilter, CancellationToken.None);

        await act.Should().ThrowAsync<MyZenNotConnectedException>();
        _myZen.Requests.Should().BeEmpty();
    }

    [Fact(DisplayName = "One invoice is read from its page")]
    public async Task GetInvoice_ReadsPage()
    {
        // Arrange
        await ConnectAsync();
        _myZen.Respond(HttpMethod.Get, "/Order/Cost/Invoice/152375962/EditOrder", MyZenInvoicePages.PurchaseDetails);

        // Act
        var invoice = await Gateway().GetInvoiceAsync("purchase", "invoice", 152375962, CancellationToken.None);

        // Assert
        invoice!.Counterparty.Should().Be("Anthropic, PBC");
        invoice.Lines.Should().ContainSingle();
        _myZen.Requests.Single().Method.Should().Be("GET");
    }

    [Fact(DisplayName = "An unknown invoice is null and keeps the session connected")]
    public async Task GetInvoice_Unknown_Null()
    {
        // Arrange
        await ConnectAsync();
        _myZen.Respond(HttpMethod.Get, "/Order/Income/CreditNote/9/EditOrder", MyZenInvoicePages.NotFound);

        // Act
        var invoice = await Gateway().GetInvoiceAsync("sales", "creditNote", 9, CancellationToken.None);

        // Assert
        invoice.Should().BeNull();
        _store.Record!.State.Should().Be(MyZenSessionState.Connected);
    }
}
