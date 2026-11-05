using Clercq.It.Application.Features.MyZen;

namespace Clercq.It.Application.Tests.Features.MyZen;

public class MyZenCompanyValidatorsTests
{
    [Theory(DisplayName = "VAT numbers are normalized and Belgian ones pass the modulo-97 check")]
    [InlineData("BE 0825.170.684", "BE0825170684", true)]
    [InlineData("0662.728.645", "BE0662728645", true)]
    [InlineData("be0825170685", "BE0825170685", false)]
    [InlineData("BE 825.170.684", "BE825170684", false)]
    [InlineData("NL 8530.43.871.B01", "NL853043871B01", true)]
    [InlineData("X1", "X1", false)]
    public void Vat_NormalizedAndChecked(string typed, string normalized, bool valid)
    {
        MyZenVat.Normalize(typed).Should().Be(normalized);
        MyZenVat.IsValid(normalized).Should().Be(valid);
    }

    [Fact(DisplayName = "The acceptance customer is valid")]
    public void CreateCustomer_Acceptance_Valid()
    {
        var command = new CreateMyZenCustomerCommand(new MyZenCustomerInput(
            Name: "In The Pocket NV", Vat: "BE 0825.170.684", Street: "Sassevaartstraat", StreetNumber: "46", Box: "401",
            Zipcode: "9000", City: "Gent", Email: "billing@inthepocket.example"));

        new CreateMyZenCustomerCommandValidator().Validate(command).IsValid.Should().BeTrue();
    }

    [Theory(DisplayName = "A new customer needs a name, a valid VAT unless private, and valid optional fields")]
    [InlineData(null, "BE0825170684", null, null, null, null, "Name is required")]
    [InlineData("Acme", null, null, null, null, null, "VAT number is required*")]
    [InlineData("Acme", "BE0825170685", null, null, null, null, "VAT number 'BE0825170685' is not valid*")]
    [InlineData("Acme", "BE0825170684", "not-an-email", null, null, null, "Email must be*")]
    [InlineData("Acme", "BE0825170684", null, "900", null, null, "A Belgian postal code has four digits")]
    [InlineData("Acme", "BE0825170684", null, null, "Belgium", null, "Country must be*")]
    [InlineData("Acme", "BE0825170684", null, null, null, "Shop", "Type must be Company or Private")]
    public void CreateCustomer_Invalid(string? name, string? vat, string? email, string? zip, string? country, string? type, string message)
    {
        var command = new CreateMyZenCustomerCommand(new MyZenCustomerInput(
            Name: name, Vat: vat, Email: email, Zipcode: zip, Country: country, Type: type));

        var result = new CreateMyZenCustomerCommandValidator().Validate(command);

        result.Errors.Select(e => e.ErrorMessage).Should().ContainMatch(message);
    }

    [Fact(DisplayName = "A private customer needs no VAT; a Dutch postal code is not checked as Belgian")]
    public void CreateCustomer_PrivateForeign_Valid()
    {
        var command = new CreateMyZenCustomerCommand(new MyZenCustomerInput(
            Name: "Jan Peeters", Type: "Private", Zipcode: "1012 AB", Country: "NL", Email: "a@b.example; c@d.example"));

        new CreateMyZenCustomerCommandValidator().Validate(command).IsValid.Should().BeTrue();
    }

    [Fact(DisplayName = "An update needs a positive ID and at least one field")]
    public void UpdateCustomer_Empty_Invalid()
    {
        new UpdateMyZenCustomerCommandValidator().Validate(new UpdateMyZenCustomerCommand(1, new MyZenCustomerInput()))
            .Errors.Select(e => e.ErrorMessage).Should().Contain("Pass at least one field to change");
        new UpdateMyZenCustomerCommandValidator().Validate(new UpdateMyZenCustomerCommand(0, new MyZenCustomerInput(Name: "X")))
            .IsValid.Should().BeFalse();
        new UpdateMyZenProjectCommandValidator().Validate(new UpdateMyZenProjectCommand(1, new MyZenProjectInput()))
            .IsValid.Should().BeFalse();
    }

    [Fact(DisplayName = "A new project needs a name, a customer and a start date")]
    public void CreateProject_Required()
    {
        var result = new CreateMyZenProjectCommandValidator().Validate(new CreateMyZenProjectCommand(new MyZenProjectInput()));

        result.Errors.Select(e => e.ErrorMessage).Should().Contain(["Name is required",
            "Customer ID is required; find it with search_myzen_customers", "Start date is required (yyyy-MM-dd)"]);
    }

    [Theory(DisplayName = "Project status, dates and amounts are checked")]
    [InlineData("Bezig", "2026-12-01", null, 0, true)]
    [InlineData("InProgress", "2026-12-01", "2026-12-31", 10, true)]
    [InlineData("Busy", "2026-12-01", null, 0, false)]
    [InlineData("Klaar", "2026-12-01", "2026-11-30", 0, false)]
    [InlineData("Prospect", "2026-12-01", null, -1, false)]
    public void CreateProject_Checked(string status, string start, string? end, decimal budget, bool valid)
    {
        var project = new MyZenProjectInput("In The Pocket", 888, status, DateOnly.Parse(start, System.Globalization.CultureInfo.InvariantCulture),
            end is null ? null : DateOnly.Parse(end, System.Globalization.CultureInfo.InvariantCulture), Budget: budget);

        new CreateMyZenProjectCommandValidator().Validate(new CreateMyZenProjectCommand(project)).IsValid.Should().Be(valid);
    }

    [Theory(DisplayName = "Project statuses map between MyZen's labels and form values")]
    [InlineData("Bezig", "InProgress", "Bezig")]
    [InlineData("klaar", "Finished", "Klaar")]
    [InlineData("Cancelled", "Canceled", "Geannuleerd")]
    [InlineData("Prospect", "Prospect", "Prospect")]
    public void ProjectStatus_Maps(string typed, string value, string label)
    {
        MyZenProjectStatus.ToValue(typed).Should().Be(value);
        MyZenProjectStatus.ToLabel(value).Should().Be(label);
    }
}
