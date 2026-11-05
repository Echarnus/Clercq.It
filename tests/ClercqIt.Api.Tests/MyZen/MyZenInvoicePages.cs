namespace ClercqIt.Api.Tests.MyZen;

/// <summary>
/// Trimmed copies of MyZen's invoice pages, with made-up data, in the shape mapped
/// in issue #494. Update these when MyZen changes its pages.
/// </summary>
internal static class MyZenInvoicePages
{
    /// <summary>The sales invoice list: no category column.</summary>
    public static string SalesList(params int[] pages) => $"""
        <form id="gridform" method="get"><input name="F.CurrentPage" type="hidden" value="0" /></form>
        <table class="table"><thead><tr>
          <th></th><th class="w30px"></th><th class="d-md-none">Details</th>
          <th sortcolumn="OrderNumber">Nr</th><th sortcolumn="OrderDate">Datum</th><th sortcolumn="ExpiryDate">Vervaldatum</th>
          <th sortcolumn="Reference">Referentie</th><th sortcolumn="Title">Betreft</th><th sortcolumn="CustomerName">Klant</th>
          <th sortcolumn="TotalPriceExcl">Excl.</th><th sortcolumn="TotalPriceVAT">Btw</th><th sortcolumn="TotalPriceIncl">Totaal</th>
          <th sortcolumn="ToPay">Te betalen</th><th sortcolumn="Information"></th><th sortcolumn="OrderStatusTC">Status</th>
        </tr></thead><tbody>
          <tr class="alt click" navigateurl="/Order/Income/Invoice/153382396/EditOrder">
            <td><input type="checkbox" name="OrderID" value="153382396"></td><td><a class="showPDFInvoice"></a></td>
            <td><strong>10036</strong> | Acme BV <div class="status statusTypeToPay"><span>Te betalen</span></div></td>
            <td>10036</td><td>30/10/2026</td><td>29/11/2026</td><td>PO-7</td><td></td><td>Acme BV</td>
            <td>€ 12.967,5000</td><td>€ 2.723,1800</td><td>€ 15.690,6800</td><td>€ 15.690,6800</td><td></td>
            <td><div class="status statusTypeToPay"> <span class="statusLeft">Te betalen</span><span class="statusRight"><span>24 d</span></span></div></td>
          </tr>
          <tr class="click" navigateurl="/Order/Income/Invoice/92409842/EditOrder">
            <td></td><td></td><td></td>
            <td>10017</td><td>1/07/2026</td><td>31/07/2026</td><td></td><td>Consulting</td><td>Globex NV</td>
            <td>€ 250,0000</td><td>€ 52,5000</td><td>€ 302,5000</td><td>€ 0,0000</td><td></td>
            <td><div class="status statusTypePaid"> <span class="statusLeft">Betaald</span></div></td>
          </tr>
          <tr class="click"><td>No link</td></tr>
        </tbody></table>
        <ul class="pagination" data-form="#gridform">{string.Concat(pages.Select(p => $"<li><a href=\"#\" data-prop=\"#F_CurrentPage\" data-page=\"{p}\" class=\"page-link\">{p + 1}</a></li>"))}</ul>
        """;

    /// <summary>The purchase invoice list: a category icon column before the number.</summary>
    public const string PurchaseList = """
        <table class="table"><thead><tr>
          <th></th><th></th><th>Details</th><th></th>
          <th sortcolumn="OrderNumber">Nr</th><th sortcolumn="OrderDate">Datum</th><th sortcolumn="ExpiryDate">Vervaldatum</th>
          <th sortcolumn="Reference">Referentie</th><th sortcolumn="Title">Betreft</th><th sortcolumn="CustomerName">Leverancier</th>
          <th sortcolumn="TotalPriceExcl">Excl.</th><th sortcolumn="TotalPriceVAT">Btw</th><th sortcolumn="TotalPriceIncl">Totaal</th>
          <th sortcolumn="ToPay">Te betalen</th><th sortcolumn="Information"></th><th sortcolumn="OrderStatusTC">Status</th>
        </tr></thead><tbody>
          <tr class="alt click" navigateurl="/Order/Cost/Invoice/152375962/EditOrder">
            <td></td><td></td><td></td>
            <td class="w30px"><i class="fa-solid fa-file-circle-info" title="Informaticamateriaal en software abonnementen"></i></td>
            <td>29177938</td><td>25/10/2026</td><td>25/10/2026</td><td></td><td></td><td>Anthropic, PBC</td>
            <td>€ 10,7300</td><td>€ 2,2600</td><td>€ 12,9900</td><td>€ 0,0000</td><td></td>
            <td><div class="status statusTypePaid"> <span class="statusLeft">Betaald</span></div></td>
          </tr>
        </tbody></table>
        """;

    /// <summary>A credit note list: the linked invoice is a second OrderNumber column.</summary>
    public const string CreditNoteList = """
        <table class="table"><thead><tr>
          <th></th><th></th><th></th><th></th>
          <th sortcolumn="OrderNumber">Nr</th><th sortcolumn="OrderNumber">Factuur</th><th sortcolumn="OrderDate">Datum</th>
          <th sortcolumn="ExpiryDate">Vervaldatum</th><th sortcolumn="Reference">Referentie</th><th sortcolumn="Title">Betreft</th>
          <th sortcolumn="CustomerName">Leverancier</th><th sortcolumn="TotalPriceExcl">Excl.</th><th sortcolumn="TotalPriceVAT">Btw</th>
          <th sortcolumn="TotalPriceIncl">Totaal</th><th sortcolumn="ToPay">Te betalen</th><th sortcolumn="Information"></th>
          <th sortcolumn="OrderStatusTC">Status</th>
        </tr></thead><tbody>
          <tr class="alt click" navigateurl="/Order/Cost/CreditNote/117189643/EditOrder">
            <td></td><td></td><td></td><td></td>
            <td>CN-0007</td><td>INV-0042</td><td>24/05/2026</td><td>24/05/2026</td><td></td><td></td><td>Initech</td>
            <td>€ -100,0000</td><td>€ -21,0000</td><td>€ -121,0000</td><td>€ 0,0000</td><td></td>
            <td><div class="status statusTypePaid"></div></td>
          </tr>
        </tbody></table>
        """;

    /// <summary>A sent sales invoice: read-only, with plain text next to each label.</summary>
    public const string SalesDetails = """
        <html><body>
        <input name="Order.OrderID" type="hidden" value="153382396" />
        <input name="Order.OrderDirectionTC" type="hidden" value="Income" />
        <h5>FACTUUR 10036 <span class="badge statusTypeToPay mx-2">Te betalen</span></h5>
        <div class="row form-group"><div class="col-5 col-form-label"><label for="Order_OrderNumber">Factuurnummer</label></div>
          <div class="col-7"><span class="form-control-plaintext">10036</span></div></div>
        <div class="row form-group"><div class="col-5 col-form-label"><label for="CustomerName">Klant</label></div>
          <div class="col-7"><div class="form-control-plaintext"><a href="" id="btnCustomerDetails"> Acme BV<i class="fas fa-eye ml-2"></i></a></div></div></div>
        <div class="row form-group"><div class="col-5 col-form-label"><label for="Order_OrderDate">Datum</label></div><div class="col-7">30/10/2026</div></div>
        <div class="row form-group"><div class="col-5 col-form-label"><label for="Order_ExpiryDate">Vervaldatum</label></div><div class="col-7">29/11/2026</div></div>
        <div class="row form-group"><div class="col-5 col-form-label"><label for="Order_DeliveryDate">Leveringsdatum</label></div><div class="col-7">30/10/2026</div></div>
        <div class="row form-group"><div class="col-5 col-form-label"><label for="Order_OrderTitle">Betreft</label></div><div class="col-7"></div></div>
        <div class="row form-group"><div class="col-5 col-form-label"><label for="Order_Reference">Uw referentie</label></div><div class="col-7"> PO-7 </div></div>
        <input name="ProjectName" type="hidden" value="Acme Website" /><input name="ProjectID" type="hidden" value="555" />
        <select name="Order.CurrencyTC"><option value="EUR" selected>EUR</option><option value="USD">USD</option></select>
        <input name="Order.CurrencyTC" type="hidden" value="EUR" />
        <input name="Order.Paid" type="checkbox" class="custom-control-input" /><input name="Order.Paid" type="hidden" value="false" />
        <input name="Order.PaidDate" type="text" value="" />
        <table class="table table-hover"><thead><tr><th>Product</th><th>Excl.</th><th>Btw %</th><th>Incl.</th><th>Hoev.</th><th>Eenh.</th><th>Korting %</th><th>Tot. Excl.</th><th>Tot. Incl.</th></tr></thead><tbody>
          <tr class="alt">
            <td><input name="OrderLines[0].OrderLineID" type="hidden" value="594324375"></td>
            <td><input name="OrderLines[0].TotalVAT" type="hidden" value="52,50000"></td>
            <td><input name="OrderLines[0].TotalExcl" type="hidden" value="250,00000"></td>
            <td><input name="OrderLines[0].TotalIncl" type="hidden" value="302,5000"></td>
            <td><input name="OrderLines[0].Deleted" type="hidden" value="False"></td>
            <td><input name="OrderLines[0].AmountExcl" type="hidden" value="250,0000"><input name="OrderLines[0].VAT" type="hidden" value="21,00">
              <input name="OrderLines[0].Quantity" type="hidden" value="1,0000"><input name="OrderLines[0].Unit" type="hidden" value="">
              <input name="OrderLines[0].ReductionPercentage" type="hidden" value=""><input name="OrderLines[0].DescriptionExtended" type="hidden" value="Extra"></td>
            <td class="whiteSpaceNormal"> Cloud costs   October </td><td class="decimal">250,0000</td><td class="decimal">21,00</td>
          </tr>
          <tr class="alt">
            <td><input name="OrderLines[1].OrderLineID" type="hidden" value="594324376"><input name="OrderLines[1].Deleted" type="hidden" value="True"></td>
            <td class="whiteSpaceNormal">Removed line</td>
          </tr>
          <tr class="alt">
            <td><input name="OrderLines[2].OrderLineID" type="hidden" value="594324377"><input name="OrderLines[2].Deleted" type="hidden" value="False">
              <input name="OrderLines[2].AmountExcl" type="hidden" value="5,0000"><input name="OrderLines[2].VAT" type="hidden" value="21,00">
              <input name="OrderLines[2].Quantity" type="hidden" value="1,0000"><input name="OrderLines[2].ReductionPercentage" type="hidden" value="10">
              <input name="OrderLines[2].TotalExcl" type="hidden" value="5,00000"><input name="OrderLines[2].TotalVAT" type="hidden" value="1,05000">
              <input name="OrderLines[2].TotalIncl" type="hidden" value="6,0500"></td>
            <td class="whiteSpaceNormal">Administration</td>
          </tr>
        </tbody></table>
        <div class="p-3 borderTop vatliable">
          <div class="row"><div class="col-6">Totaal exclusief</div><div class="col-6 text-right totalexcl"><span>255,0000</span></div></div>
          <div class="row"><div class="col-6">Btw</div><div class="col-6 text-right totalvat"><span>53,5500</span></div></div>
          <div class="row"><div class="col-6">Totaal</div><div class="col-6 text-right totaltotal"><span>308,5500</span></div></div>
        </div>
        </body></html>
        """;

    /// <summary>A purchase invoice: an editable form.</summary>
    public const string PurchaseDetails = """
        <html><body><form id="EditOrderForm">
        <span class="badge statusTypePaid mx-2">Betaald</span>
        <input name="Order.OrderID" type="hidden" value="152375962" />
        <input name="Order.OrderDirectionTC" type="radio" value="Cost" checked /><input name="Order.OrderDirectionTC" type="radio" value="Income" />
        <div class="row form-group"><div class="col-lg-5"><label for="Order_OrderNumber">Factuurnummer</label></div>
          <div class="col-lg-7"><input class="form-control" name="Order.OrderNumber" value="29177938" /></div></div>
        <input name="CustomerName" type="hidden" value="Anthropic, PBC" /><input name="CustomerID" type="hidden" value="29643266" />
        <input name="Order.OrderDate" value="25/10/2026" /><input name="Order.ExpiryDate" value="8/11/2026" />
        <input name="Order.DeliveryDate" value="" /><input name="Order.OrderTitle" value="Claude Max" /><input name="Order.Reference" value="" />
        <input name="ProjectName" type="hidden" value="" />
        <select name="Order.CurrencyTC"><option value="EUR">EUR</option><option value="USD" selected>USD</option></select>
        <input name="Order.Paid" type="checkbox" class="custom-control-input" value="true" checked /><input name="Order.Paid" type="hidden" value="false" />
        <input name="Order.PaidDate" value="26/10/2026" />
        <input name="AccountingCostCategoryName" value="Informaticamateriaal en software abonnementen" />
        <table><thead><tr><th>Omschrijving</th></tr></thead><tbody>
          <tr class="alt invoiceOrderLine">
            <td><input name="OrderLines[0].OrderLineID" type="hidden" value="589213364"><input name="OrderLines[0].TotalVAT" type="hidden" value="2,25620">
              <input name="OrderLines[0].TotalExcl" type="hidden" value="10,74380"><input name="OrderLines[0].TotalIncl" type="hidden" value="13,0000">
              <input name="OrderLines[0].Deleted" type="hidden" value="False"></td>
            <td><textarea name="OrderLines[0].Description">Claude Max plan</textarea></td>
            <td><input name="OrderLines[0].AmountExcl" value="10,7438"></td><td><input name="OrderLines[0].VAT" value="21,0000"></td>
            <td><input name="OrderLines[0].Quantity" value="1,0000"></td><td><input name="OrderLines[0].Unit" value="month"></td>
            <td><input name="OrderLines[0].ReductionPercentage" value=""></td>
          </tr>
        </tbody></table>
        <div class="totalexcl">10,7438</div><div class="totalvat">2,2562</div><div class="totaltotal">13,0000</div>
        </form></body></html>
        """;

    /// <summary>What MyZen shows for an id that is not one of the company's documents.</summary>
    public const string NotFound = "<html><body><h1>Er is een fout opgetreden</h1></body></html>";
}
