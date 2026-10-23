using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Clercq.It.Application.Features.MyZen;
using Clercq.It.Infrastructure.MyZen;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace ClercqIt.Api.Tests.MyZen;

/// <summary>
/// The /api/myzen endpoints with the MyZen flag on and a mocked gateway, signed in
/// as the realm's admin (the only user with MyZen.Owner).
/// </summary>
[Collection(ApiTestGroup.Name)]
public class MyZenEndpointTests(CustomWebApplicationFactory factory) : IAsyncLifetime
{
    private readonly Mock<IMyZenGateway> _gateway = new();
    private WebApplicationFactory<Program>? _app;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _app = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("FeatureFlags:MyZen", "true");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IMyZenGateway>();
                services.AddScoped(_ => _gateway.Object);
            });
        });
        _client = _app.CreateClient();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", await factory.GetTokenAsync("admin"));
    }

    public async Task DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }
    }

    [Fact(DisplayName = "MyZen endpoints are 404 while the MyZen flag is off")]
    public async Task FlagOff_NotFound()
    {
        var client = await factory.CreateClientForUserAsync("admin");

        var response = await client.GetAsync("/api/myzen/session");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "GET /api/myzen/session returns the session state")]
    public async Task GetSession_ReturnsState()
    {
        // Arrange
        _gateway.Setup(g => g.GetStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MyZenStatusDto(MyZenSessionState.Expired, null, null));

        // Act
        var status = await _client.GetFromJsonAsync<MyZenStatusDto>("/api/myzen/session");

        // Assert
        status!.State.Should().Be(MyZenSessionState.Expired);
    }

    [Fact(DisplayName = "The connect steps return the next login step")]
    public async Task ConnectSteps_ReturnNextStep()
    {
        // Arrange
        _gateway.Setup(g => g.StartLoginAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MyZenLoginStepDto(MyZenLoginStep.Credentials, "data:image/png;base64,AA==", null));
        _gateway.Setup(g => g.SubmitLoginAsync("ab12", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MyZenLoginStepDto(MyZenLoginStep.Mfa, null, null));
        _gateway.Setup(g => g.SubmitMfaCodeAsync("123456", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MyZenLoginStepDto(MyZenLoginStep.Connected, null, null));

        // Act
        var start = await (await _client.PostAsync("/api/myzen/session/start", null)).Content.ReadFromJsonAsync<MyZenLoginStepDto>();
        var login = await (await _client.PostAsJsonAsync("/api/myzen/session/login", new { captcha = "ab12" })).Content.ReadFromJsonAsync<MyZenLoginStepDto>();
        var mfa = await (await _client.PostAsJsonAsync("/api/myzen/session/mfa", new { code = "123456" })).Content.ReadFromJsonAsync<MyZenLoginStepDto>();

        // Assert
        start!.CaptchaImage.Should().StartWith("data:image/png");
        login!.Step.Should().Be(MyZenLoginStep.Mfa);
        mfa!.Step.Should().Be(MyZenLoginStep.Connected);
    }

    [Fact(DisplayName = "An MFA code that is not six digits returns 400")]
    public async Task Mfa_InvalidCode_BadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/myzen/session/mfa", new { code = "12" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _gateway.Verify(g => g.SubmitMfaCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "DELETE /api/myzen/session disconnects and returns 204")]
    public async Task Disconnect_NoContent()
    {
        var response = await _client.DeleteAsync("/api/myzen/session");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        _gateway.Verify(g => g.DisconnectAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "GET /api/myzen/projects returns the projects")]
    public async Task GetProjects_Ok()
    {
        // Arrange
        _gateway.Setup(g => g.GetProjectsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new MyZenProjectDto(5, "Website", "Acme", null, null, "Bezig")]);

        // Act
        var projects = await _client.GetFromJsonAsync<List<MyZenProjectDto>>("/api/myzen/projects");

        // Assert
        projects.Should().ContainSingle().Which.Id.Should().Be(5);
    }

    [Fact(DisplayName = "An expired session returns 409, an unreachable MyZen 502")]
    public async Task Errors_MappedToProblems()
    {
        // Arrange
        _gateway.SetupSequence(g => g.GetProjectsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new MyZenNotConnectedException())
            .ThrowsAsync(new MyZenUnavailableException("down"));

        // Act
        var expired = await _client.GetAsync("/api/myzen/projects");
        var down = await _client.GetAsync("/api/myzen/projects");

        // Assert
        expired.StatusCode.Should().Be(HttpStatusCode.Conflict);
        down.StatusCode.Should().Be(HttpStatusCode.BadGateway);
    }

    [Fact(DisplayName = "GET /api/myzen/time-entries returns the period's entries")]
    public async Task GetTimeEntries_Ok()
    {
        // Arrange
        var from = new DateOnly(2026, 9, 1);
        var to = new DateOnly(2026, 9, 30);
        _gateway.Setup(g => g.GetTimeEntriesAsync(from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new MyZenTimeEntryDto(1, from, "Work", null, "Website", null, "Acme", TimeSpan.FromHours(8), false)]);

        // Act
        var entries = await _client.GetFromJsonAsync<List<MyZenTimeEntryDto>>("/api/myzen/time-entries?from=2026-09-01&to=2026-09-30");

        // Assert
        entries.Should().ContainSingle().Which.Duration.Should().Be(TimeSpan.FromHours(8));
    }

    [Fact(DisplayName = "A period that ends before it starts returns 400")]
    public async Task GetTimeEntries_BadPeriod_BadRequest()
    {
        var response = await _client.GetAsync("/api/myzen/time-entries?from=2026-09-30&to=2026-09-01");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "GET /api/myzen/time-entries/{id} returns the entry or 404")]
    public async Task GetTimeEntry_OkOrNotFound()
    {
        // Arrange
        _gateway.Setup(g => g.GetTimeEntryAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MyZenTimeEntryDto(1, new DateOnly(2026, 9, 1), "Work", 5, "Website", 7, "Acme", TimeSpan.FromHours(8), true));

        // Act
        var found = await _client.GetAsync("/api/myzen/time-entries/1");
        var missing = await _client.GetAsync("/api/myzen/time-entries/2");

        // Assert
        found.StatusCode.Should().Be(HttpStatusCode.OK);
        missing.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

/// <summary>The EF session store against the real PostgreSQL test database.</summary>
[Collection(ApiTestGroup.Name)]
public class EfMyZenSessionStoreTests(CustomWebApplicationFactory factory) : IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "The session store saves, updates, loads and deletes the one session")]
    public async Task SaveLoadDelete_RoundTrip()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IMyZenSessionStore>();
        var now = DateTimeOffset.UtcNow;

        // Act & Assert
        (await store.LoadAsync(CancellationToken.None)).Should().BeNull();

        await store.SaveAsync(new MyZenSessionRecord { State = MyZenSessionState.Connecting, Payload = [1, 2], UpdatedAt = now }, CancellationToken.None);
        await store.SaveAsync(new MyZenSessionRecord
        {
            State = MyZenSessionState.Connected,
            Payload = [3],
            ConnectedAt = now,
            LastSeenAlive = now,
            UpdatedAt = now,
        }, CancellationToken.None);

        var loaded = await store.LoadAsync(CancellationToken.None);
        loaded!.State.Should().Be(MyZenSessionState.Connected);
        loaded.Payload.Should().Equal(3);
        loaded.ConnectedAt.Should().BeCloseTo(now, TimeSpan.FromMilliseconds(1));

        await store.DeleteAsync(CancellationToken.None);
        (await store.LoadAsync(CancellationToken.None)).Should().BeNull();
    }
}
