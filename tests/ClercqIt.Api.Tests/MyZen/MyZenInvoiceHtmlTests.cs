using Clercq.It.Application.Features.MyZen;
using Clercq.It.Infrastructure.MyZen;
using FluentAssertions;

namespace ClercqIt.Api.Tests.MyZen;

/// <summary>Reading MyZen's invoice pages (issue #494).</summary>
public class MyZenInvoiceHtmlTests
{
    [Fact(DisplayName = "The sales list gives each invoice with Belgian amounts, status and the next page")]
    public void ParseInvoiceList_Sales()
    {
        // Act
        var list = MyZenInvoiceHtml.ParseInvoiceList(MyZenInvoicePages.SalesList(0, 1), "sales", "invoice", 0);

        // Assert
        list.HasNextPage.Should().BeTrue();
        list.Invoices.Should().Equal(
            new MyZenInvoiceSummaryDto(153382396, "sales", "invoice", "10036", new DateOnly(2026, 10, 30), new DateOnly(2026, 11, 29),
                "PO-7", null, "Acme BV", 12967.5m, 2723.18m, 15690.68m, 15690.68m, "ToPay", null),
            new MyZenInvoiceSummaryDto(92409842, "sales", "invoice", "10017", new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 31),
                null, "Consulting", "Globex NV", 250m, 52.5m, 302.5m, 0m, "Paid", null));
    }

    [Fact(DisplayName = "The last page of a list has no next page")]
    public void ParseInvoiceList_LastPage()
    {
        MyZenInvoiceHtml.ParseInvoiceList(MyZenInvoicePages.SalesList(0, 1), "sales", "invoice", 1).HasNextPage.Should().BeFalse();
    }

    [Fact(DisplayName = "The purchase list finds its columns past the category icon and gives the category")]
    public void ParseInvoiceList_Purchase()
    {
        var invoice = MyZenInvoiceHtml.ParseInvoiceList(MyZenInvoicePages.PurchaseList, "purchase", "invoice", 0).Invoices.Single();

        invoice.Should().Be(new MyZenInvoiceSummaryDto(152375962, "purchase", "invoice", "29177938", new DateOnly(2026, 10, 25),
            new DateOnly(2026, 10, 25), null, null, "Anthropic, PBC", 10.73m, 2.26m, 12.99m, 0m, "Paid",
            "Informaticamateriaal en software abonnementen"));
    }

    [Fact(DisplayName = "A credit note list takes its own number, not the invoice's, and keeps negative amounts")]
    public void ParseInvoiceList_CreditNote()
    {
        var note = MyZenInvoiceHtml.ParseInvoiceList(MyZenInvoicePages.CreditNoteList, "purchase", "creditNote", 0).Invoices.Single();

        note.Number.Should().Be("CN-0007");
        note.Kind.Should().Be("creditNote");
        note.TotalIncl.Should().Be(-121m);
        note.Counterparty.Should().Be("Initech");
    }

    [Fact(DisplayName = "A page without an invoice table is an empty list")]
    public void ParseInvoiceList_NoTable_Empty()
    {
        var list = MyZenInvoiceHtml.ParseInvoiceList(MyZenInvoicePages.NotFound, "sales", "invoice", 0);

        list.Invoices.Should().BeEmpty();
        list.HasNextPage.Should().BeFalse();
    }

    [Fact(DisplayName = "A read-only sales invoice gives its header, totals and lines, without deleted lines")]
    public void ParseInvoiceDetails_Sales()
    {
        // Act
        var invoice = MyZenInvoiceHtml.ParseInvoiceDetails(MyZenInvoicePages.SalesDetails, "sales", "invoice", 153382396)!;

        // Assert
        invoice.Should().BeEquivalentTo(new
        {
            Id = 153382396L,
            Direction = "sales",
            Kind = "invoice",
            Number = "10036",
            Date = new DateOnly(2026, 10, 30),
            DueDate = new DateOnly(2026, 11, 29),
            DeliveryDate = new DateOnly(2026, 10, 30),
            Reference = "PO-7",
            Subject = (string?)null,
            Counterparty = "Acme BV",
            CounterpartyId = (long?)null,
            Project = "Acme Website",
            Currency = "EUR",
            TotalExcl = 255m,
            Vat = 53.55m,
            TotalIncl = 308.55m,
            Status = "ToPay",
            Paid = false,
            PaidDate = (DateOnly?)null,
            Category = (string?)null,
        });
        invoice.Lines.Should().Equal(
            new MyZenInvoiceLineDto("Cloud costs October", 1m, null, 250m, 21m, null, 250m, 52.5m, 302.5m),
            new MyZenInvoiceLineDto("Administration", 1m, null, 5m, 21m, 10m, 5m, 1.05m, 6.05m));
    }

    [Fact(DisplayName = "An editable purchase invoice gives its form values, payment and cost category")]
    public void ParseInvoiceDetails_Purchase()
    {
        // Act
        var invoice = MyZenInvoiceHtml.ParseInvoiceDetails(MyZenInvoicePages.PurchaseDetails, "purchase", "invoice", 152375962)!;

        // Assert
        invoice.Number.Should().Be("29177938");
        invoice.Counterparty.Should().Be("Anthropic, PBC");
        invoice.CounterpartyId.Should().Be(29643266);
        invoice.DueDate.Should().Be(new DateOnly(2026, 11, 8));
        invoice.DeliveryDate.Should().BeNull();
        invoice.Subject.Should().Be("Claude Max");
        invoice.Project.Should().BeNull();
        invoice.Currency.Should().Be("USD");
        invoice.Paid.Should().BeTrue();
        invoice.PaidDate.Should().Be(new DateOnly(2026, 10, 26));
        invoice.Status.Should().Be("Paid");
        invoice.Category.Should().Be("Informaticamateriaal en software abonnementen");
        invoice.TotalIncl.Should().Be(13m);
        invoice.Lines.Should().Equal(
            new MyZenInvoiceLineDto("Claude Max plan", 1m, "month", 10.7438m, 21m, null, 10.7438m, 2.2562m, 13m));
    }

    [Fact(DisplayName = "A page for another id or an error page is no invoice")]
    public void ParseInvoiceDetails_OtherOrMissing_Null()
    {
        MyZenInvoiceHtml.ParseInvoiceDetails(MyZenInvoicePages.PurchaseDetails, "purchase", "invoice", 1).Should().BeNull();
        MyZenInvoiceHtml.ParseInvoiceDetails(MyZenInvoicePages.NotFound, "purchase", "invoice", 152375962).Should().BeNull();
    }

    [Theory(DisplayName = "Belgian amounts are read with thousands dots, decimal commas and signs")]
    [InlineData("€ 12.967,5000", "12967.5")]
    [InlineData("-10,74380", "-10.7438")]
    [InlineData("€ -1.000,00", "-1000")]
    [InlineData("21,00", "21")]
    [InlineData("", "0")]
    [InlineData("n/a", "0")]
    public void ParseAmount_Belgian(string text, string expected)
    {
        MyZenInvoiceHtml.ParseAmount(text).Should().Be(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture));
    }
}
