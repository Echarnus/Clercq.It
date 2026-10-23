using Clercq.It.Application.Features.MyZen;

namespace Clercq.It.Application.Tests.Features.MyZen;

public class MyZenHandlersTests
{
    private readonly Mock<IMyZenGateway> _gateway = new();

    [Fact(DisplayName = "The status query returns the gateway's session state")]
    public async Task GetStatus_ReturnsGatewayStatus()
    {
        // Arrange
        var status = new MyZenStatusDto(MyZenSessionState.Connected, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        _gateway.Setup(g => g.GetStatusAsync(MoqIt.IsAny<CancellationToken>())).ReturnsAsync(status);

        // Act
        var result = await new GetMyZenStatusQueryHandler(_gateway.Object).Handle(new GetMyZenStatusQuery(), CancellationToken.None);

        // Assert
        result.Should().Be(status);
    }

    [Fact(DisplayName = "The projects query returns the gateway's projects")]
    public async Task GetProjects_ReturnsGatewayProjects()
    {
        // Arrange
        IReadOnlyList<MyZenProjectDto> projects = [new(1, "Website", "Acme", "01/01/2026", null, "Bezig")];
        _gateway.Setup(g => g.GetProjectsAsync(MoqIt.IsAny<CancellationToken>())).ReturnsAsync(projects);

        // Act
        var result = await new GetMyZenProjectsQueryHandler(_gateway.Object).Handle(new GetMyZenProjectsQuery(), CancellationToken.None);

        // Assert
        result.Should().BeSameAs(projects);
    }

    [Fact(DisplayName = "The time entries query passes its period to the gateway")]
    public async Task GetTimeEntries_PassesPeriod()
    {
        // Arrange
        var from = new DateOnly(2026, 9, 1);
        var to = new DateOnly(2026, 9, 30);
        IReadOnlyList<MyZenTimeEntryDto> entries = [Entry(7)];
        _gateway.Setup(g => g.GetTimeEntriesAsync(from, to, MoqIt.IsAny<CancellationToken>())).ReturnsAsync(entries);

        // Act
        var result = await new GetMyZenTimeEntriesQueryHandler(_gateway.Object).Handle(new GetMyZenTimeEntriesQuery(from, to), CancellationToken.None);

        // Assert
        result.Should().BeSameAs(entries);
    }

    [Fact(DisplayName = "The time entry query returns the gateway's entry")]
    public async Task GetTimeEntry_ReturnsGatewayEntry()
    {
        // Arrange
        var entry = Entry(7);
        _gateway.Setup(g => g.GetTimeEntryAsync(7, MoqIt.IsAny<CancellationToken>())).ReturnsAsync(entry);

        // Act
        var result = await new GetMyZenTimeEntryQueryHandler(_gateway.Object).Handle(new GetMyZenTimeEntryQuery(7), CancellationToken.None);

        // Assert
        result.Should().Be(entry);
    }

    [Fact(DisplayName = "Starting the login returns the gateway's first step")]
    public async Task StartLogin_ReturnsFirstStep()
    {
        // Arrange
        var step = new MyZenLoginStepDto(MyZenLoginStep.Credentials, "data:image/png;base64,AA==", null);
        _gateway.Setup(g => g.StartLoginAsync(MoqIt.IsAny<CancellationToken>())).ReturnsAsync(step);

        // Act
        var result = await new StartMyZenLoginCommandHandler(_gateway.Object).Handle(new StartMyZenLoginCommand(), CancellationToken.None);

        // Assert
        result.Should().Be(step);
    }

    [Theory(DisplayName = "The login passes a trimmed CAPTCHA, or none when it is blank")]
    [InlineData(" ab12 ", "ab12")]
    [InlineData("   ", null)]
    [InlineData(null, null)]
    public async Task SubmitLogin_TrimsCaptcha(string? typed, string? expected)
    {
        // Arrange
        var step = new MyZenLoginStepDto(MyZenLoginStep.Mfa, null, null);
        _gateway.Setup(g => g.SubmitLoginAsync(expected, MoqIt.IsAny<CancellationToken>())).ReturnsAsync(step);

        // Act
        var result = await new SubmitMyZenLoginCommandHandler(_gateway.Object).Handle(new SubmitMyZenLoginCommand(typed), CancellationToken.None);

        // Assert
        result.Should().Be(step);
    }

    [Fact(DisplayName = "The MFA code goes to the gateway")]
    public async Task SubmitMfaCode_PassesCode()
    {
        // Arrange
        var step = new MyZenLoginStepDto(MyZenLoginStep.Connected, null, null);
        _gateway.Setup(g => g.SubmitMfaCodeAsync("123456", MoqIt.IsAny<CancellationToken>())).ReturnsAsync(step);

        // Act
        var result = await new SubmitMyZenMfaCodeCommandHandler(_gateway.Object).Handle(new SubmitMyZenMfaCodeCommand("123456"), CancellationToken.None);

        // Assert
        result.Should().Be(step);
    }

    [Fact(DisplayName = "Disconnecting asks the gateway to forget the session")]
    public async Task Disconnect_CallsGateway()
    {
        // Act
        await new DisconnectMyZenCommandHandler(_gateway.Object).Handle(new DisconnectMyZenCommand(), CancellationToken.None);

        // Assert
        _gateway.Verify(g => g.DisconnectAsync(MoqIt.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "The MyZen exceptions say what the owner must do")]
    public void Exceptions_HaveMessages()
    {
        new MyZenNotConnectedException().Message.Should().Contain("Connect it again");
        new MyZenUnavailableException("down").Message.Should().Be("down");
    }

    private static MyZenTimeEntryDto Entry(long id) =>
        new(id, new DateOnly(2026, 9, 1), "Work", null, "Website", null, "Acme", TimeSpan.FromHours(8), false);
}
