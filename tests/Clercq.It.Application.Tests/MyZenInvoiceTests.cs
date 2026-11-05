using System.Globalization;
using Clercq.It.Application.Features.MyZen;

namespace Clercq.It.Application.Tests.Features.MyZen;

/// <summary>The invoice queries (issue #494): validation and the filters applied to MyZen's results.</summary>
public class MyZenInvoiceTests
{
    private static readonly DateOnly From = new(2026, 1, 1);
    private static readonly DateOnly To = new(2026, 12, 31);

    private readonly Mock<IMyZenInvoiceGateway> _gateway = new();

    [Theory(DisplayName = "A list filter needs a known direction, kind and status, and a period of up to 366 days")]
    [InlineData("sales", "invoice", "2026-01-01", "2026-12-31", null, true)]
    [InlineData("purchase", "creditNote", "2024-01-01", "2024-12-31", "Paid", true)]
    [InlineData("purchase", "invoice", "2026-01-01", "2027-01-01", null, true)]
    [InlineData("purchase", "invoice", "2026-01-01", "2027-01-02", null, false)]
    [InlineData("purchase", "invoice", "2026-02-01", "2026-01-01", null, false)]
    [InlineData("income", "invoice", "2026-01-01", "2026-01-31", null, false)]
    [InlineData("sales", "offer", "2026-01-01", "2026-01-31", null, false)]
    [InlineData("sales", "invoice", "2026-01-01", "2026-01-31", "Betaald", false)]
    public void ListFilter_Validated(string direction, string kind, string from, string to, string? status, bool valid)
    {
        // Arrange
        var query = new GetMyZenInvoicesQuery(new MyZenInvoiceFilter(direction, kind,
            DateOnly.Parse(from, CultureInfo.InvariantCulture), DateOnly.Parse(to, CultureInfo.InvariantCulture), Status: status));

        // Act
        var result = new GetMyZenInvoicesQueryValidator().Validate(query);

        // Assert
        result.IsValid.Should().Be(valid);
    }

    [Fact(DisplayName = "A minimum amount above the maximum is refused")]
    public void ListFilter_MinAboveMax_Invalid()
    {
        var query = new GetMyZenInvoicesQuery(new MyZenInvoiceFilter("sales", "invoice", From, To, MinAmount: 100, MaxAmount: 10));

        var result = new GetMyZenInvoicesQueryValidator().Validate(query);

        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "'minAmount' must not be above 'maxAmount'");
    }

    [Theory(DisplayName = "One invoice needs a known direction and kind and a positive id")]
    [InlineData("sales", "invoice", 1, true)]
    [InlineData("purchase", "creditNote", 42, true)]
    [InlineData("purchase", "invoice", 0, false)]
    [InlineData("cost", "invoice", 1, false)]
    [InlineData("sales", "Invoice", 1, false)]
    public void GetInvoice_Validated(string direction, string kind, long id, bool valid)
    {
        var result = new GetMyZenInvoiceQueryValidator().Validate(new GetMyZenInvoiceQuery(direction, kind, id));

        result.IsValid.Should().Be(valid);
    }

    [Fact(DisplayName = "The list passes MyZen's filters on and applies counterparty and amount itself")]
    public async Task List_FiltersCounterpartyAndAmount()
    {
        // Arrange
        MyZenInvoiceFilter? sent = null;
        _gateway.Setup(g => g.GetInvoicesAsync(MoqIt.IsAny<MyZenInvoiceFilter>(), MoqIt.IsAny<CancellationToken>()))
            .Callback<MyZenInvoiceFilter, CancellationToken>((f, _) => sent = f)
            .ReturnsAsync([Invoice(1, "Anthropic, PBC", 21.78m), Invoice(2, "Anthropic, PBC", 242m), Invoice(3, "Proton AG", 12.99m)]);
        var filter = new MyZenInvoiceFilter("purchase", "invoice", From, To, Search: "  ", Status: "Paid",
            Counterparty: " anthropic ", MinAmount: 20, MaxAmount: 100);

        // Act
        var result = await new GetMyZenInvoicesQueryHandler(_gateway.Object)
            .Handle(new GetMyZenInvoicesQuery(filter), CancellationToken.None);

        // Assert
        result.Select(i => i.Id).Should().Equal(1);
        sent!.Search.Should().BeNull();
        sent.Status.Should().Be("Paid");
    }

    [Fact(DisplayName = "Without extra filters the list returns every invoice MyZen gave")]
    public async Task List_NoFilters_ReturnsAll()
    {
        _gateway.Setup(g => g.GetInvoicesAsync(MoqIt.IsAny<MyZenInvoiceFilter>(), MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync([Invoice(1, "Acme", 10m), Invoice(2, "Globex", -5m)]);

        var result = await new GetMyZenInvoicesQueryHandler(_gateway.Object)
            .Handle(new GetMyZenInvoicesQuery(new MyZenInvoiceFilter("sales", "invoice", From, To)), CancellationToken.None);

        result.Should().HaveCount(2);
    }

    [Fact(DisplayName = "The invoice query asks the gateway for that direction, kind and id")]
    public async Task Get_PassesIdentity()
    {
        await new GetMyZenInvoiceQueryHandler(_gateway.Object)
            .Handle(new GetMyZenInvoiceQuery("sales", "creditNote", 7), CancellationToken.None);

        _gateway.Verify(g => g.GetInvoiceAsync("sales", "creditNote", 7, MoqIt.IsAny<CancellationToken>()));
    }

    private static MyZenInvoiceSummaryDto Invoice(long id, string counterparty, decimal totalIncl) =>
        new(id, "purchase", "invoice", id.ToString(CultureInfo.InvariantCulture), From, From, null, null, counterparty,
            totalIncl, 0m, totalIncl, 0m, "Paid", null);
}
