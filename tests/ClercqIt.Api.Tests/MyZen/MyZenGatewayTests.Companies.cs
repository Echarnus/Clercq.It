using System.Net;
using Clercq.It.Application.Features.MyZen;
using Clercq.It.Infrastructure.MyZen;
using FluentAssertions;

namespace ClercqIt.Api.Tests.MyZen;

/// <summary>Creating and changing customers and projects (issue #495) against the scripted MyZen.</summary>
public partial class MyZenGatewayTests
{
    private static readonly MyZenCustomerInput NewCustomer = new(
        Name: "In The Pocket NV", Vat: "BE0825170684", Street: "Sassevaartstraat", StreetNumber: "46", Box: "401",
        Zipcode: "9000", City: "Gent", Email: "billing@inthepocket.example");

    [Fact(DisplayName = "Creating a customer posts MyZen's whole new-customer form with the given fields, the token and the save action")]
    public async Task CreateCustomer_PostsWholeForm()
    {
        // Arrange
        await ConnectAsync();
        _myZen.Respond(HttpMethod.Get, "/Company/Customer/CompanyEdit", MyZenPages.CustomerPage(0));
        _myZen.Respond(HttpMethod.Post, "/Company/Customer/CompanyEdit", MyZenPages.CustomerPage(888, saved: true));

        // Act
        var id = await Gateway().CreateCustomerAsync(NewCustomer, CancellationToken.None);

        // Assert
        id.Should().Be(888);
        var save = _myZen.Requests[1];
        save.Headers.Should().NotContainKey("RequestVerificationToken");
        var form = Fields(save.Form);
        form.Should().Contain(new KeyValuePair<string, string>("ButtonActionType", "Save"))
            .And.Contain(new KeyValuePair<string, string>("__RequestVerificationToken", "form-token"))
            .And.Contain(new KeyValuePair<string, string>("Company.CompanyName", "In The Pocket NV"))
            .And.Contain(new KeyValuePair<string, string>("Company.VAT", "BE0825170684"))
            .And.Contain(new KeyValuePair<string, string>("InvoiceAddress.Street", "Sassevaartstraat"))
            .And.Contain(new KeyValuePair<string, string>("InvoiceAddress.Box", "401"))
            .And.Contain(new KeyValuePair<string, string>("Company.Email", "billing@inthepocket.example"))
            // Fields the tool was not given keep the form's values.
            .And.Contain(new KeyValuePair<string, string>("Company.LanguageTC", "NL"))
            .And.Contain(new KeyValuePair<string, string>("CustomerTypeTC", "Company"))
            .And.Contain(new KeyValuePair<string, string>("IsCustomer", "True"));
        form.Where(f => f.Key == "Company.VATLiable").Select(f => f.Value).Should().Equal("true", "false");
        form.Should().NotContain(f => f.Key == "CustomerTypeTC" && f.Value == "Private");
    }

    [Fact(DisplayName = "Updating a customer changes only the given fields and unticks a checkbox the MVC way")]
    public async Task UpdateCustomer_ChangesOnlyGivenFields()
    {
        // Arrange
        await ConnectAsync();
        _myZen.Respond(HttpMethod.Get, "/Company/Customer/777/CompanyEdit", MyZenPages.CustomerPage(777));
        _myZen.Respond(HttpMethod.Post, "/Company/Customer/777/CompanyEdit", MyZenPages.CustomerPage(777, saved: true));

        // Act
        var found = await Gateway().UpdateCustomerAsync(777, new MyZenCustomerInput(Email: "new@acme.example", VatLiable: false),
            CancellationToken.None);

        // Assert
        found.Should().BeTrue();
        var form = Fields(_myZen.Requests[1].Form);
        form.Should().Contain(new KeyValuePair<string, string>("Company.Email", "new@acme.example"))
            .And.Contain(new KeyValuePair<string, string>("Company.CompanyName", "Acme NV"))
            .And.Contain(new KeyValuePair<string, string>("Company.Nr", "2"))
            .And.Contain(new KeyValuePair<string, string>("InvoiceAddress.City", "Gent"));
        form.Where(f => f.Key == "Company.VATLiable").Select(f => f.Value).Should().Equal("false");
    }

    [Fact(DisplayName = "Updating an unknown customer posts nothing")]
    public async Task UpdateCustomer_Unknown_False()
    {
        // Arrange
        await ConnectAsync();
        _myZen.Respond(HttpMethod.Get, "/Company/Customer/1/CompanyEdit", "<html><title>Error</title></html>", finalPath: "/Home/NoAccess");

        // Act
        var found = await Gateway().UpdateCustomerAsync(1, new MyZenCustomerInput(Name: "X"), CancellationToken.None);

        // Assert
        found.Should().BeFalse();
        _myZen.Requests.Should().ContainSingle(r => r.Method == "GET");
    }

    [Fact(DisplayName = "A validation message on the returned page is MyZen's refusal")]
    public async Task SaveForm_ValidationError_Rejected()
    {
        // Arrange
        await ConnectAsync();
        _myZen.Respond(HttpMethod.Get, "/Company/Customer/CompanyEdit", MyZenPages.CustomerPage(0));
        _myZen.Respond(HttpMethod.Post, "/Company/Customer/CompanyEdit", MyZenPages.CustomerPage(0, error: "Btw-nummer is ongeldig"));

        // Act
        var act = () => Gateway().CreateCustomerAsync(NewCustomer, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<MyZenRejectedException>().WithMessage("Btw-nummer is ongeldig");
        _store.Record!.State.Should().Be(MyZenSessionState.Connected);
    }

    [Fact(DisplayName = "A returned page without the success message is not taken as saved")]
    public async Task SaveForm_NotConfirmed_Unavailable()
    {
        // Arrange
        await ConnectAsync();
        _myZen.Respond(HttpMethod.Get, "/Project/555/EditProject", MyZenPages.ProjectPage(555));
        _myZen.Respond(HttpMethod.Post, "/Project/555/EditProject", MyZenPages.ProjectPage(555));

        // Act
        var act = () => Gateway().UpdateProjectAsync(555, new MyZenProjectInput(Status: MyZenProjectStatus.Finished), null,
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<MyZenUnavailableException>().WithMessage("*did not confirm*");
    }

    [Fact(DisplayName = "An error status from MyZen on a save is unavailable")]
    public async Task SaveForm_ErrorStatus_Unavailable()
    {
        // Arrange
        await ConnectAsync();
        _myZen.Respond(HttpMethod.Get, "/Project/555/EditProject", MyZenPages.ProjectPage(555));
        _myZen.Respond(HttpMethod.Post, "/Project/555/EditProject", "Sorry", HttpStatusCode.InternalServerError);

        // Act
        var act = () => Gateway().UpdateProjectAsync(555, new MyZenProjectInput(Name: "X"), null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<MyZenUnavailableException>().WithMessage("*500*");
    }

    [Fact(DisplayName = "Creating a project posts MyZen's project form with its dates, amounts, status, customer and save action")]
    public async Task CreateProject_PostsForm()
    {
        // Arrange
        await ConnectAsync();
        _myZen.Respond(HttpMethod.Get, "/Project/AddProject", MyZenPages.ProjectPage(0));
        _myZen.Respond(HttpMethod.Post, "/Project/AddProject", MyZenPages.ProjectPage(999, saved: true));
        var project = new MyZenProjectInput("In The Pocket", 888, MyZenProjectStatus.Prospect, new DateOnly(2026, 12, 1),
            Budget: 12500.5m, TimeBudgetHours: 7.5m, Rate: 95m);

        // Act
        var id = await Gateway().CreateProjectAsync(project, "In The Pocket NV", CancellationToken.None);

        // Assert
        id.Should().Be(999);
        var form = Fields(_myZen.Requests[1].Form);
        form.Should().Contain(new KeyValuePair<string, string>("ButtonActionType", "BulkinputSaveReceipt"))
            .And.Contain(new KeyValuePair<string, string>("ProjectName", "In The Pocket"))
            .And.Contain(new KeyValuePair<string, string>("CustomerID", "888"))
            .And.Contain(new KeyValuePair<string, string>("CustomerName", "In The Pocket NV"))
            .And.Contain(new KeyValuePair<string, string>("StartDate", "01/12/2026"))
            .And.Contain(new KeyValuePair<string, string>("Budget", "12500,50"))
            .And.Contain(new KeyValuePair<string, string>("Rate", "95,00"))
            .And.Contain(new KeyValuePair<string, string>("TimeBudget", "27000"))
            .And.Contain(new KeyValuePair<string, string>("TimeBudgetInput", "07:30"))
            .And.Contain(new KeyValuePair<string, string>("Reference", "PO-1"));
        form.Where(f => f.Key == "ProjectStatus").Select(f => f.Value).Should().Equal("Prospect");
    }

    [Fact(DisplayName = "Updating a project keeps its customer unless a new one is given")]
    public async Task UpdateProject_KeepsCustomer()
    {
        // Arrange
        await ConnectAsync();
        _myZen.Respond(HttpMethod.Get, "/Project/555/EditProject", MyZenPages.ProjectPage(555));
        _myZen.Respond(HttpMethod.Post, "/Project/555/EditProject", MyZenPages.ProjectPage(555, saved: true));

        // Act
        var found = await Gateway().UpdateProjectAsync(555, new MyZenProjectInput(Status: MyZenProjectStatus.InProgress), null,
            CancellationToken.None);

        // Assert
        found.Should().BeTrue();
        var form = Fields(_myZen.Requests[1].Form);
        form.Should().Contain(new KeyValuePair<string, string>("CustomerID", "777"))
            .And.Contain(new KeyValuePair<string, string>("StartDate", "1/12/2026 0:00:00"))
            .And.Contain(new KeyValuePair<string, string>("Notes", "Phase 1"));
        form.Where(f => f.Key == "ProjectStatus").Select(f => f.Value).Should().Equal("InProgress");
    }

    [Fact(DisplayName = "Updating an unknown project posts nothing")]
    public async Task UpdateProject_Unknown_False()
    {
        // Arrange
        await ConnectAsync();
        _myZen.Respond(HttpMethod.Get, "/Project/1/EditProject", "<html><title>Sorry</title></html>", HttpStatusCode.InternalServerError);

        // Act
        var found = await Gateway().UpdateProjectAsync(1, new MyZenProjectInput(Name: "X"), null, CancellationToken.None);

        // Assert
        found.Should().BeFalse();
        _myZen.Requests.Should().ContainSingle();
    }

    [Fact(DisplayName = "An expired session on a save throws NotConnected")]
    public async Task SaveForm_Expired_Throws()
    {
        // Arrange
        await ConnectAsync();
        _myZen.Respond(HttpMethod.Get, "/Project/AddProject", MyZenPages.Login(), finalPath: "/Account/Logon");

        // Act
        var act = () => Gateway().CreateProjectAsync(new MyZenProjectInput("X", 1), "Acme", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<MyZenNotConnectedException>();
    }

    [Fact(DisplayName = "The form reader posts what a browser would: checked boxes, selected options, textareas, no buttons")]
    public void Form_ReadsLikeBrowser()
    {
        var form = MyZenForm.Parse("""
            <form id="pageform">
              <input name="A" value="1" /><input name="B" type="checkbox" value="true" /><input name="B" type="hidden" value="false" />
              <input name="C" type="radio" value="x" /><input name="C" type="radio" value="y" checked />
              <select name="D"><option value="d1">One</option><option value="d2">Two</option></select>
              <textarea name="E">text</textarea><input name="F" type="submit" value="go" /><input name="G" value="g" disabled />
            </form>
            """)!;

        form.Fields.Select(f => $"{f.Key}={f.Value}").Should().Equal("A=1", "B=false", "C=y", "D=d1", "E=text");
        MyZenForm.Parse("<html></html>").Should().BeNull();
    }

    private static List<KeyValuePair<string, string>> Fields(string form) =>
        form.Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split('=', 2))
            .Select(p => KeyValuePair.Create(WebUtility.UrlDecode(p[0]), WebUtility.UrlDecode(p.Length > 1 ? p[1] : string.Empty)))
            .ToList();
}
