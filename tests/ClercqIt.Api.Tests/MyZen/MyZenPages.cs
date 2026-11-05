namespace ClercqIt.Api.Tests.MyZen;

/// <summary>
/// Trimmed copies of MyZen's markup, with made-up data, in the shape mapped in
/// issue #345. Update these when MyZen changes its pages.
/// </summary>
internal static class MyZenPages
{
    public const string CaptchaImage = "data:image/png;base64,iVBORw0KGgo=";

    public static string Login(bool captcha = false, string? error = null) => $$"""
        <html><body>
        <script type="application/json" id="LogonTokenConfig">{"csrfToken":"<input name=\"__RequestVerificationToken\" type=\"hidden\" value=\"form-token\" />","rawcsrfToken":"header-token"}</script>
        <form id="loginform" method="post">
          <div class="alert alert-danger{{(error is null ? " d-none" : "")}}">{{error}}</div>
          <input name="Email" type="email" /><input name="Password" type="password" />
          <div class="mt-3 captcha{{(captcha ? "" : " d-none")}}"><div class="d-flex"><img src="{{CaptchaImage}}" alt="Captcha"></div>
            <input id="Captcha" name="Captcha" class="form-control" type="text" /></div>
          <input name="CaptchaCheck" type="hidden" value="captcha-check" />
          <input name="RememberMe" type="checkbox" value="true" />
        </form>
        </body></html>
        """;

    // As MyZen serves it: the token is in the layout config script; the hidden
    // token inputs only appear once MyZen's scripts run in a browser (issue #448).
    public const string TimeRegistrationIndex = """
        <html><body>
        <script type="application/json" id="layoutmainconfig">{"csrfToken":"<input name=\"__RequestVerificationToken\" type=\"hidden\" value=\"input-token\" />","rawcsrfToken":"page-token","numberOfDecimals":2}</script>
        <form id="pageform" method="post"></form>
        <form id="gridform" method="get">
          <input name="F.SortColumn" type="hidden" value="" />
          <input name="F.SortDescending" type="hidden" value="True" />
          <input name="F.CurrentPage" type="hidden" value="0" />
          <input name="F.DateFrom" value="01/10/2026" />
          <input name="F.DateTill" value="31/10/2026" />
          <select name="F.UserID"><option value="">All</option><option value="4242" selected>Me</option></select>
          <input name="F.Invoiced" value="" />
        </form>
        </body></html>
        """;

    public static string EntryRow(long id, string date, string description, string customer, string project, int seconds, bool invoiced) => $"""
        <tr class="alt">
          <td><input name="TimeRegistrationEntries[0].TimeRegistrationID" type="hidden" value="{id}" /></td>
          <td><input name="TimeRegistrationEntries[0].Running" type="hidden" value="False" /></td>
          <td><input name="TimeRegistrationEntries[0].UserID" type="hidden" value="4242" /></td>
          <td style="width:40px"><input type="checkbox" name="TimeRegistrationID" value="{id}"></td>
          <td class="maxw300px d-none d-md-table-cell"><div class="text-muted d-flex"><div class="font-weight-bold w90px">{date}</div><div class="ml-2">owner@example.com</div></div><div class="text-truncate"><div class="text-muted">{description}</div></div></td>
          <td class="maxw200px d-md-none"><div class="badge badge-light">{date}</div><div class="text-truncate">{customer}</div></td>
          <td class="text-right d-md-none"><span class="timer" data-running="false" data-duration="1"></span></td>
          <td class="text-nowrap d-none d-md-table-cell">{customer}</td>
          <td class="text-nowrap d-none d-md-table-cell">{project}</td>
          <td class="text-nowrap d-none d-md-table-cell">{(invoiced ? "<div class=\"badge badge-success\">Gefactureerd</div>" : "")}</td>
          <td class="text-right d-none d-md-table-cell"><button class="startTimeRegistration" data-id="{id}"></button></td>
          <td class="w90px d-none d-md-table-cell"><span class="timer" data-running="false" data-duration="{seconds}"><span>08:00</span></span></td>
        </tr>
        """;

    public static string EntryList(IEnumerable<string> rows, params int[] pages) => $"""
        <table><thead><tr><th>Omschrijving</th></tr></thead><tbody>{string.Concat(rows)}</tbody></table>
        <ul class="pagination">{string.Concat(pages.Select(p => $"<li><a href=\"\" data-prop=\"#F_CurrentPage\" data-page=\"{p}\" class=\"page-link\">{p + 1}</a></li>"))}</ul>
        """;

    public static string EntryDetails(long id, string date = "15/09/2026 0:00:00", string duration = "07:30", bool invoiced = true) => $"""
        <form>
          <input name="EditTimeRegistration.TimeRegistrationID" type="hidden" value="{id}" />
          <input value="Acme Website" /><input name="EditTimeRegistration.ProjectName" type="hidden" value="Acme Website" />
          <input name="EditTimeRegistration.ProjectID" type="hidden" value="555" />
          <input value="Acme NV" /><input name="EditTimeRegistration.CustomerName" type="hidden" value="Acme NV" />
          <input name="EditTimeRegistration.CustomerID" type="hidden" value="777" />
          <input name="EditTimeRegistration.Date" value="{date}" />
          <input name="EditTimeRegistration.Duration" value="{duration}" />
          <textarea name="EditTimeRegistration.Description">Sprint review</textarea>
          <input name="EditTimeRegistration.Invoiced" type="checkbox" value="true" {(invoiced ? "checked" : "")} />
          <input name="EditTimeRegistration.Invoiced" type="hidden" value="false" />
        </form>
        """;

    public static string ProjectList(params int[] pages) => $"""
        <table><thead><tr><th>Project</th></tr></thead><tbody>
          <tr class="alt click" id="ProjectID-555" navigateurl="/Project/555/Dashboard">
            <td>Acme Website</td><td>Acme NV</td><td>01/01/2026</td><td></td>
            <td><span class="currencyValue">1.000</span></td><td class="w90px"><div class="badge badge-info">Bezig</div></td><td></td>
          </tr>
          <tr class="alt click" id="ProjectID-notanumber"><td>Broken</td></tr>
        </tbody></table>
        <ul class="pagination">{string.Concat(pages.Select(p => $"<li><a data-page=\"{p}\">{p + 1}</a></li>"))}</ul>
        """;

    public const string ProjectSuggestions = """
        [{"value":"20788","label":"Acme Website[~][~]777[~]Acme NV"},{"value":23200,"label":"Vakantie[~][~][~]"},
         {"value":"23201","label":"Ziekte[~][~][extradata-CustomerID][~][extradata-CustomerName]"}]
        """;

    /// <summary>The customer list (<c>/Company/Customer</c>): the ID is in the row's navigateurl.</summary>
    public static string CustomerList(params int[] pages) => $"""
        <table><thead><tr><th></th><th>Creatie op</th><th>Nr</th><th>Naam</th><th>Bedrijf</th><th>Contact</th><th>Gemeente</th><th>E-mail</th><th>Telefoon</th></tr></thead><tbody>
          <tr class="alt click" navigateurl="/Company/Customer/777/CompanyEdit?ReturnTo=%2FCompany%2FCustomer">
            <td></td><td>2025-06-16T13:39:41</td><td>2</td><td>Acme NV</td><td>BE0123456749</td><td></td><td>9000 Gent</td><td>billing@acme.example</td><td></td><td></td><td></td><td>Peppol</td><td></td>
          </tr>
          <tr class="alt click" navigateurl="/Company/Customer/778/CompanyEdit"><td></td><td></td><td>3</td><td>Beta BV</td></tr>
          <tr class="alt click" navigateurl="/Home/Somewhere"><td>Broken</td></tr>
        </tbody></table>
        <ul class="pagination">{string.Concat(pages.Select(p => $"<li><a data-page=\"{p}\">{p + 1}</a></li>"))}</ul>
        """;

    /// <summary>A customer's edit page (<c>/Company/Customer/{id}/CompanyEdit</c>), trimmed to the fields read.</summary>
    public static string CustomerDetails(long id) => $"""
        <form id="pageform" method="post">
          <input name="ButtonActionType" type="hidden" value="None" />
          <input name="Company.CompanyID" type="hidden" value="{id}" />
          <input name="IsCustomer" type="hidden" value="True" />
          <input name="CustomerTypeTC" type="radio" value="Company" checked="checked" /><input name="CustomerTypeTC" type="radio" value="Private" />
          <input name="Company.VAT" type="hidden" value="BE0123456749" />
          <input name="Company.VATLiable" type="checkbox" value="true" checked="checked" /><input name="Company.VATLiable" type="hidden" value="false" />
          <input name="Company.CompanyName" type="text" value="Acme NV" />
          <input name="Company.CommercialName" type="text" value="" />
          <input name="Company.Nr" type="text" value="2" />
          <input name="Company.Reference" type="text" value="" />
          <input name="Company.ContactFirstName" type="text" value="Ada" />
          <input name="Company.ContactLastName" type="text" value="" />
          <textarea name="Company.Email">billing@acme.example</textarea>
          <input name="Company.Website" type="text" value="" />
          <input name="Company.Phone" type="tel" value="+32 9 123 45 67" />
          <input name="Company.Mobile" type="tel" value="" />
          <select name="Company.LanguageTC"><option value="FR">Frans</option><option selected="selected" value="NL">Nederlands</option></select>
          <select name="InvoiceAddress.Country"><option value="NL">Nederland</option><option selected="selected" value="BE">België</option></select>
          <input name="InvoiceAddress.Street" type="hidden" value="Voorbeeldstraat" />
          <input name="InvoiceAddress.StreetNumber" type="text" value="12" />
          <input name="InvoiceAddress.Box" type="text" value="3" />
          <input name="InvoiceAddress.Zipcode" type="hidden" value="9000" />
          <input name="InvoiceAddress.City" type="hidden" value="Gent" />
        </form>
        """;

    /// <summary>A project's edit page (<c>/Project/{id}/EditProject</c>), trimmed to the fields read.</summary>
    public static string ProjectDetails(long id, string status = "InProgress", string customerId = "777", string customer = "Acme NV") => $"""
        <form id="pageform" method="post">
          <input name="ProjectID" type="hidden" value="{id}" />
          <input name="ButtonActionType" type="hidden" value="None" />
          <input name="ProjectStatus" type="radio" value="Prospect" {(status == "Prospect" ? "checked=\"checked\"" : "")} />
          <input name="ProjectStatus" type="radio" value="InProgress" {(status == "InProgress" ? "checked=\"checked\"" : "")} />
          <input name="ProjectStatus" type="radio" value="Finished" />
          <input name="ProjectStatus" type="radio" value="Canceled" />
          <input name="ProjectName" type="text" value="Acme Website" />
          <input type="text" value="{customer}" />
          <input name="CustomerName" type="hidden" value="{customer}" />
          <input name="CustomerID" type="hidden" value="{customerId}" />
          <input name="Reference" type="text" value="PO-1" />
          <input name="StartDate" type="text" value="1/12/2026 0:00:00" />
          <input name="EndDate" type="text" value="" />
          <textarea name="Notes">Phase 1</textarea>
          <input name="Budget" type="text" value="1.000,50" />
          <input name="TimeBudgetInput" type="text" value="10:30" />
          <input name="TimeBudget" type="hidden" value="37800" />
          <input name="Rate" type="text" value="95,0000" />
        </form>
        """;

    /// <summary>
    /// A whole edit page around a form: the layout config with the anti-forgery token, the red
    /// "also on this page" badge MyZen always shows, the save buttons, and after a save the
    /// success alert or a validation message.
    /// </summary>
    public static string EditPage(string form, string saveAction, bool saved = false, string? error = null) => $$"""
        <html><body>
        <script type="application/json" id="layoutmainconfig">{"csrfToken":"<input name=\"__RequestVerificationToken\" type=\"hidden\" value=\"form-token\" />","rawcsrfToken":"page-token"}</script>
        <div class="alert alert-danger float-right p-2 mr-3">Ook op deze pagina:</div>
        <div class="d-none alert alert-danger">Je bent uitgelogd.</div>
        {{(saved ? "<div class=\"alert alert-success\">De aanpassingen zijn succesvol opgeslagen.</div>" : "")}}
        {{(error is null ? "" : $"<span class=\"field-validation-error\">{error}</span>")}}
        {{form}}
        <button buttonactiontype="Delete" class="btnSubmitActionType" type="button">Wissen</button>
        <button buttonactiontype="{{saveAction}}" class="btn-primary btn btnSubmitActionType" type="button">Opslaan</button>
        </body></html>
        """;

    public static string CustomerPage(long id, bool saved = false, string? error = null) => EditPage(CustomerDetails(id), "Save", saved, error);

    public static string ProjectPage(long id, bool saved = false, string? error = null) =>
        EditPage(ProjectDetails(id), "BulkinputSaveReceipt", saved, error);
}
