using Clercq.It.Application.Features.MyZen;

namespace Clercq.It.Application.Tests.Features.MyZen;

/// <summary>The customer and project write rules (issue #495) against a mocked gateway: duplicates, look-ups and read-back.</summary>
public class MyZenCompanyCommandsTests
{
    private static readonly MyZenCustomerDto Pocket = new(888, "3", "In The Pocket NV", null, "Company", "BE0825170684", true, null, null,
        null, null, "NL", null, null, null, new MyZenAddressDto("Sassevaartstraat", "46", "401", "9000", "Gent", "BE"));

    private static readonly MyZenProjectDetailsDto PocketProject = new(999, "In The Pocket", 888, "In The Pocket NV", "Prospect",
        new DateOnly(2026, 12, 1), null, null, null, null, null, null);

    private readonly Mock<IMyZenGateway> _gateway = new();

    [Fact(DisplayName = "Creating a customer sends the normalized VAT number and returns the customer read back")]
    public async Task CreateCustomer_NormalizesAndReadsBack()
    {
        // Arrange
        _gateway.Setup(g => g.GetCustomersAsync("BE0825170684", MoqIt.IsAny<CancellationToken>())).ReturnsAsync([]);
        _gateway.Setup(g => g.CreateCustomerAsync(MoqIt.IsAny<MyZenCustomerInput>(), MoqIt.IsAny<CancellationToken>())).ReturnsAsync(888);
        _gateway.Setup(g => g.GetCustomerAsync(888, MoqIt.IsAny<CancellationToken>())).ReturnsAsync(Pocket);

        // Act
        var customer = await new CreateMyZenCustomerCommandHandler(_gateway.Object).Handle(
            new CreateMyZenCustomerCommand(new MyZenCustomerInput(Name: " In The Pocket NV ", Vat: "BE 0825.170.684", Country: "be")),
            CancellationToken.None);

        // Assert
        customer.Should().BeSameAs(Pocket);
        var sent = new MyZenCustomerInput(Name: "In The Pocket NV", Vat: "BE0825170684", Country: "BE");
        _gateway.Verify(g => g.CreateCustomerAsync(sent, MoqIt.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "A customer with the same VAT number is reported and nothing is created")]
    public async Task CreateCustomer_DuplicateVat_Refused()
    {
        // Arrange
        _gateway.Setup(g => g.GetCustomersAsync("BE0825170684", MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync([new MyZenCustomerSummaryDto(888, "3", "ITP Agency", "BE0825170684", null, "9000 Gent", null, null)]);

        // Act
        var act = () => new CreateMyZenCustomerCommandHandler(_gateway.Object).Handle(
            new CreateMyZenCustomerCommand(new MyZenCustomerInput(Name: "In The Pocket NV", Vat: "0825.170.684")), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<MyZenRejectedException>().WithMessage("*BE0825170684*'ITP Agency' (id 888)*");
        _gateway.Verify(g => g.CreateCustomerAsync(MoqIt.IsAny<MyZenCustomerInput>(), MoqIt.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "A private customer without VAT is checked on its name")]
    public async Task CreateCustomer_PrivateSameName_Refused()
    {
        // Arrange
        _gateway.Setup(g => g.GetCustomersAsync("Jan Peeters", MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync([new MyZenCustomerSummaryDto(5, null, "jan  peeters", null, null, null, null, null)]);

        // Act
        var act = () => new CreateMyZenCustomerCommandHandler(_gateway.Object).Handle(
            new CreateMyZenCustomerCommand(new MyZenCustomerInput(Name: "Jan Peeters", Type: "private")), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<MyZenRejectedException>().WithMessage("*named*(id 5)*");
    }

    [Fact(DisplayName = "When MyZen's answer has no ID the new customer is found by its VAT number")]
    public async Task CreateCustomer_NoIdInAnswer_FindsByVat()
    {
        // Arrange
        _gateway.SetupSequence(g => g.GetCustomersAsync("BE0825170684", MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync([])
            .ReturnsAsync([new MyZenCustomerSummaryDto(888, "3", "In The Pocket NV", "BE0825170684", null, null, null, null)]);
        _gateway.Setup(g => g.CreateCustomerAsync(MoqIt.IsAny<MyZenCustomerInput>(), MoqIt.IsAny<CancellationToken>())).ReturnsAsync((long?)null);
        _gateway.Setup(g => g.GetCustomerAsync(888, MoqIt.IsAny<CancellationToken>())).ReturnsAsync(Pocket);

        // Act
        var customer = await new CreateMyZenCustomerCommandHandler(_gateway.Object).Handle(
            new CreateMyZenCustomerCommand(new MyZenCustomerInput(Name: "In The Pocket NV", Vat: "BE0825170684")), CancellationToken.None);

        // Assert
        customer.Id.Should().Be(888);
    }

    [Fact(DisplayName = "A save MyZen accepted but does not show is reported, not retried")]
    public async Task CreateCustomer_NotFoundAfterSave_Unavailable()
    {
        // Arrange
        _gateway.Setup(g => g.GetCustomersAsync("BE0825170684", MoqIt.IsAny<CancellationToken>())).ReturnsAsync([]);
        _gateway.Setup(g => g.CreateCustomerAsync(MoqIt.IsAny<MyZenCustomerInput>(), MoqIt.IsAny<CancellationToken>())).ReturnsAsync((long?)null);

        // Act
        var act = () => new CreateMyZenCustomerCommandHandler(_gateway.Object).Handle(
            new CreateMyZenCustomerCommand(new MyZenCustomerInput(Name: "In The Pocket NV", Vat: "BE0825170684")), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<MyZenUnavailableException>().WithMessage("*before trying again*");
        _gateway.Verify(g => g.CreateCustomerAsync(MoqIt.IsAny<MyZenCustomerInput>(), MoqIt.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "Changing a customer's VAT number to another customer's is refused")]
    public async Task UpdateCustomer_VatOfAnother_Refused()
    {
        // Arrange
        _gateway.Setup(g => g.GetCustomersAsync("BE0825170684", MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync([new MyZenCustomerSummaryDto(888, "3", "In The Pocket NV", "BE0825170684", null, null, null, null)]);

        // Act
        var act = () => new UpdateMyZenCustomerCommandHandler(_gateway.Object).Handle(
            new UpdateMyZenCustomerCommand(7, new MyZenCustomerInput(Vat: "BE0825170684")), CancellationToken.None);
        var same = await new UpdateMyZenCustomerCommandHandler(_gateway.Object).Handle(
            new UpdateMyZenCustomerCommand(888, new MyZenCustomerInput(Vat: "BE0825170684")), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<MyZenRejectedException>();
        same.Should().BeNull("the mocked gateway has no customer 888 to update");
    }

    [Fact(DisplayName = "Creating a project checks the customer and duplicates, then returns the project read back")]
    public async Task CreateProject_CreatesAndReadsBack()
    {
        // Arrange
        _gateway.Setup(g => g.GetCustomerAsync(888, MoqIt.IsAny<CancellationToken>())).ReturnsAsync(Pocket);
        _gateway.Setup(g => g.GetProjectsAsync(MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync([new MyZenProjectDto(20787, "In The Pocket", 7, "Other BV", null, null, "Bezig")]);
        _gateway.Setup(g => g.CreateProjectAsync(MoqIt.IsAny<MyZenProjectInput>(), "In The Pocket NV", MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(999);
        _gateway.Setup(g => g.GetProjectAsync(999, MoqIt.IsAny<CancellationToken>())).ReturnsAsync(PocketProject);

        // Act
        var project = await new CreateMyZenProjectCommandHandler(_gateway.Object).Handle(
            new CreateMyZenProjectCommand(new MyZenProjectInput("In The Pocket", 888, StartDate: new DateOnly(2026, 12, 1))),
            CancellationToken.None);

        // Assert
        project.Should().BeSameAs(PocketProject);
        var sent = new MyZenProjectInput("In The Pocket", 888, MyZenProjectStatus.Prospect, new DateOnly(2026, 12, 1));
        _gateway.Verify(g => g.CreateProjectAsync(sent, "In The Pocket NV", MoqIt.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "A project with the same name for the same customer is reported and nothing is created")]
    public async Task CreateProject_Duplicate_Refused()
    {
        // Arrange
        _gateway.Setup(g => g.GetCustomerAsync(888, MoqIt.IsAny<CancellationToken>())).ReturnsAsync(Pocket);
        _gateway.Setup(g => g.GetProjectsAsync(MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync([new MyZenProjectDto(999, "in the pocket", 888, "In The Pocket NV", "01/12/2026", null, "Prospect")]);

        // Act
        var act = () => new CreateMyZenProjectCommandHandler(_gateway.Object).Handle(
            new CreateMyZenProjectCommand(new MyZenProjectInput("In The Pocket", 888, "Bezig", new DateOnly(2026, 12, 1))),
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<MyZenRejectedException>().WithMessage("*already has a project*(id 999)*");
        _gateway.Verify(g => g.CreateProjectAsync(MoqIt.IsAny<MyZenProjectInput>(), MoqIt.IsAny<string>(), MoqIt.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact(DisplayName = "A project for an unknown customer is refused with a hint")]
    public async Task CreateProject_UnknownCustomer_Refused()
    {
        var act = () => new CreateMyZenProjectCommandHandler(_gateway.Object).Handle(
            new CreateMyZenProjectCommand(new MyZenProjectInput("X", 5, StartDate: new DateOnly(2026, 12, 1))), CancellationToken.None);

        await act.Should().ThrowAsync<MyZenRejectedException>().WithMessage("*no customer with id 5*search_myzen_customers*");
    }

    [Fact(DisplayName = "Updating a project's status sends MyZen's form value and only that field")]
    public async Task UpdateProject_Status()
    {
        // Arrange
        _gateway.Setup(g => g.GetProjectAsync(999, MoqIt.IsAny<CancellationToken>())).ReturnsAsync(PocketProject);
        _gateway.Setup(g => g.UpdateProjectAsync(999, MoqIt.IsAny<MyZenProjectInput>(), null, MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var project = await new UpdateMyZenProjectCommandHandler(_gateway.Object).Handle(
            new UpdateMyZenProjectCommand(999, new MyZenProjectInput(Status: "bezig")), CancellationToken.None);

        // Assert
        project.Should().BeSameAs(PocketProject);
        var sent = new MyZenProjectInput(Status: MyZenProjectStatus.InProgress);
        _gateway.Verify(g => g.UpdateProjectAsync(999, sent, null, MoqIt.IsAny<CancellationToken>()), Times.Once);
        _gateway.Verify(g => g.GetProjectsAsync(MoqIt.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "An end date before the project's current start date is refused")]
    public async Task UpdateProject_EndBeforeStart_Refused()
    {
        _gateway.Setup(g => g.GetProjectAsync(999, MoqIt.IsAny<CancellationToken>())).ReturnsAsync(PocketProject);

        var act = () => new UpdateMyZenProjectCommandHandler(_gateway.Object).Handle(
            new UpdateMyZenProjectCommand(999, new MyZenProjectInput(EndDate: new DateOnly(2026, 11, 30))), CancellationToken.None);

        await act.Should().ThrowAsync<MyZenRejectedException>().WithMessage("*2026-11-30*before*2026-12-01*");
    }

    [Fact(DisplayName = "Moving a project to another customer passes that customer's name and checks duplicates there")]
    public async Task UpdateProject_NewCustomer()
    {
        // Arrange
        _gateway.Setup(g => g.GetProjectAsync(999, MoqIt.IsAny<CancellationToken>())).ReturnsAsync(PocketProject with { CustomerId = 7 });
        _gateway.Setup(g => g.GetCustomerAsync(888, MoqIt.IsAny<CancellationToken>())).ReturnsAsync(Pocket);
        _gateway.Setup(g => g.GetProjectsAsync(MoqIt.IsAny<CancellationToken>())).ReturnsAsync([]);
        _gateway.Setup(g => g.UpdateProjectAsync(999, MoqIt.IsAny<MyZenProjectInput>(), "In The Pocket NV", MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await new UpdateMyZenProjectCommandHandler(_gateway.Object).Handle(
            new UpdateMyZenProjectCommand(999, new MyZenProjectInput(CustomerId: 888)), CancellationToken.None);

        // Assert
        _gateway.Verify(g => g.GetProjectsAsync(MoqIt.IsAny<CancellationToken>()), Times.Once);
        _gateway.VerifyAll();
    }

    [Fact(DisplayName = "Updating an unknown project returns null")]
    public async Task UpdateProject_Unknown_Null()
    {
        var project = await new UpdateMyZenProjectCommandHandler(_gateway.Object).Handle(
            new UpdateMyZenProjectCommand(1, new MyZenProjectInput(Name: "X")), CancellationToken.None);

        project.Should().BeNull();
    }
}
