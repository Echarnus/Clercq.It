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

    public const string TimeRegistrationIndex = """
        <html><body>
        <form method="post"><input name="__RequestVerificationToken" type="hidden" value="page-token" /></form>
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
}
