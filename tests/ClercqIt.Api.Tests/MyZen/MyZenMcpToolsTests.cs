using System.Net.Http.Headers;
using System.Text.Json;
using Clercq.It.Application.Features.MyZen;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Moq;

namespace ClercqIt.Api.Tests.MyZen;

/// <summary>
/// The MyZen tools on the MCP server (issue #394), with the real MCP client and real
/// Keycloak tokens, and a mocked gateway so MyZen itself is never called.
/// </summary>
[Collection(ApiTestGroup.Name)]
public class MyZenMcpToolsTests(CustomWebApplicationFactory factory) : IAsyncLifetime
{
    private static readonly string[] MyZenToolNames =
    [
        "myzen_status", "list_myzen_projects", "search_myzen_projects", "list_myzen_time_entries",
        "get_myzen_time_entry", "create_myzen_time_entry", "update_myzen_time_entry", "delete_myzen_time_entry",
    ];

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly Mock<IMyZenGateway> _gateway = new();
    private WebApplicationFactory<Program>? _app;

    public Task InitializeAsync()
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
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }
    }

    [Fact(DisplayName = "The MyZen owner sees the MyZen tools")]
    public async Task ListTools_Owner_SeesMyZenTools()
    {
        await using var mcp = await ConnectAsync(_app!, "admin");

        var tools = (await mcp.ListToolsAsync()).Select(t => t.Name);

        tools.Should().Contain(MyZenToolNames);
    }

    [Fact(DisplayName = "A contributor without MyZen.Owner does not see or call the MyZen tools")]
    public async Task Tools_Contributor_Hidden()
    {
        await using var mcp = await ConnectAsync(_app!, "projects");

        var tools = (await mcp.ListToolsAsync()).Select(t => t.Name);
        Func<Task> act = async () => await mcp.CallToolAsync("myzen_status");

        tools.Should().NotContain(MyZenToolNames);
        await act.Should().ThrowAsync<ModelContextProtocol.McpException>();
        _gateway.VerifyNoOtherCalls();
    }

    [Fact(DisplayName = "The MyZen tools are hidden while the MyZen flag is off")]
    public async Task ListTools_FlagOff_Hidden()
    {
        await using var mcp = await ConnectAsync(factory, "admin");

        var tools = (await mcp.ListToolsAsync()).Select(t => t.Name);

        tools.Should().NotContain(MyZenToolNames).And.Contain("list_projects");
    }

    [Fact(DisplayName = "The read tools return the gateway's data")]
    public async Task ReadTools_ReturnData()
    {
        // Arrange
        var from = new DateOnly(2026, 10, 1);
        var to = new DateOnly(2026, 10, 31);
        _gateway.Setup(g => g.GetStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MyZenStatusDto(MyZenSessionState.Connected, null, null));
        _gateway.Setup(g => g.SearchProjectsAsync("acme", It.IsAny<CancellationToken>()))
            .ReturnsAsync([new MyZenProjectOptionDto(5, "Acme Website", 7, "Acme")]);
        _gateway.Setup(g => g.GetProjectsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new MyZenProjectDto(5, "Acme Website", "Acme", null, null, "Bezig")]);
        _gateway.Setup(g => g.GetTimeEntriesAsync(from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Entry]);
        await using var mcp = await ConnectAsync(_app!, "admin");

        // Act
        var status = Json(await mcp.CallToolAsync("myzen_status"));
        var options = Json(await mcp.CallToolAsync("search_myzen_projects", Args(("query", "acme"))));
        var projects = Json(await mcp.CallToolAsync("list_myzen_projects"));
        var entries = Json(await mcp.CallToolAsync("list_myzen_time_entries", Args(("from", "2026-10-01"), ("to", "2026-10-31"))));

        // Assert
        status.GetProperty("state").GetString().Should().Be(MyZenSessionState.Connected);
        options[0].GetProperty("customerId").GetInt64().Should().Be(7);
        projects.GetArrayLength().Should().Be(1);
        entries[0].GetProperty("id").GetInt64().Should().Be(101);
    }

    [Fact(DisplayName = "Creating an entry sends it once and returns a confirmation")]
    public async Task Create_SendsEntry()
    {
        // Arrange
        await using var mcp = await ConnectAsync(_app!, "admin");

        // Act
        var result = await mcp.CallToolAsync("create_myzen_time_entry", Args(
            ("date", "2026-10-02"), ("durationMinutes", 480), ("projectId", 5), ("customerId", 7), ("description", "Work")));

        // Assert
        Text(result).Should().Be("Added the time entry on 2026-10-02.");
        _gateway.Verify(g => g.CreateTimeEntryAsync(
            new MyZenTimeEntryInput(new DateOnly(2026, 10, 2), 480, 5, 7, "Work", false), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "An invalid entry comes back as a validation error and is not sent")]
    public async Task Create_Invalid_ToolError()
    {
        await using var mcp = await ConnectAsync(_app!, "admin");

        var result = await mcp.CallToolAsync("create_myzen_time_entry", Args(
            ("date", "2026-10-02"), ("durationMinutes", 0), ("projectId", 5)));

        result.IsError.Should().BeTrue();
        Text(result).Should().Contain("Duration must be between 1 minute and 24 hours");
        _gateway.Verify(g => g.CreateTimeEntryAsync(It.IsAny<MyZenTimeEntryInput>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "Updating changes only the fields passed")]
    public async Task Update_KeepsOtherFields()
    {
        // Arrange
        _gateway.Setup(g => g.GetTimeEntryAsync(101, It.IsAny<CancellationToken>())).ReturnsAsync(Entry);
        _gateway.Setup(g => g.UpdateTimeEntryAsync(101, It.IsAny<MyZenTimeEntryInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        await using var mcp = await ConnectAsync(_app!, "admin");

        // Act
        var result = await mcp.CallToolAsync("update_myzen_time_entry", Args(("id", 101), ("durationMinutes", 240)));

        // Assert
        Text(result).Should().Be("Updated time entry 101.");
        _gateway.Verify(g => g.UpdateTimeEntryAsync(101,
            new MyZenTimeEntryInput(new DateOnly(2026, 10, 1), 240, 5, 7, "Sprint review", true), It.IsAny<CancellationToken>()));
    }

    [Fact(DisplayName = "Moving an entry to another project takes that project's customer")]
    public async Task Update_NewProject_UsesItsCustomer()
    {
        // Arrange
        _gateway.Setup(g => g.GetTimeEntryAsync(101, It.IsAny<CancellationToken>())).ReturnsAsync(Entry);
        _gateway.Setup(g => g.UpdateTimeEntryAsync(101, It.IsAny<MyZenTimeEntryInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        await using var mcp = await ConnectAsync(_app!, "admin");

        // Act
        await mcp.CallToolAsync("update_myzen_time_entry", Args(("id", 101), ("projectId", 23200)));

        // Assert
        _gateway.Verify(g => g.UpdateTimeEntryAsync(101,
            It.Is<MyZenTimeEntryInput>(i => i.ProjectId == 23200 && i.CustomerId == null), It.IsAny<CancellationToken>()));
    }

    [Fact(DisplayName = "Unknown entries and an expired session come back as tool errors")]
    public async Task Errors_AreToolErrors()
    {
        // Arrange
        _gateway.Setup(g => g.DeleteTimeEntryAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _gateway.Setup(g => g.DeleteTimeEntryAsync(2, It.IsAny<CancellationToken>())).ThrowsAsync(new MyZenNotConnectedException());
        _gateway.Setup(g => g.DeleteTimeEntryAsync(3, It.IsAny<CancellationToken>())).ThrowsAsync(new MyZenRejectedException("Invoiced"));
        await using var mcp = await ConnectAsync(_app!, "admin");

        // Act
        var unknown = await mcp.CallToolAsync("delete_myzen_time_entry", Args(("id", 1)));
        var expired = await mcp.CallToolAsync("delete_myzen_time_entry", Args(("id", 2)));
        var refused = await mcp.CallToolAsync("delete_myzen_time_entry", Args(("id", 3)));
        var missing = await mcp.CallToolAsync("get_myzen_time_entry", Args(("id", 4)));

        // Assert
        Text(unknown).Should().Be("No MyZen time entry with id 1.");
        Text(expired).Should().Contain("Connect it again");
        Text(refused).Should().Be("Invoiced");
        missing.IsError.Should().BeTrue();
    }

    [Fact(DisplayName = "Deleting an entry returns a confirmation")]
    public async Task Delete_Confirms()
    {
        _gateway.Setup(g => g.DeleteTimeEntryAsync(101, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        await using var mcp = await ConnectAsync(_app!, "admin");

        var result = await mcp.CallToolAsync("delete_myzen_time_entry", Args(("id", 101)));

        Text(result).Should().Be("Deleted time entry 101.");
    }

    private static readonly MyZenTimeEntryDto Entry =
        new(101, new DateOnly(2026, 10, 1), "Sprint review", 5, "Acme Website", 7, "Acme", TimeSpan.FromHours(8), true);

    private static Dictionary<string, object?> Args(params (string Name, object? Value)[] args) =>
        args.ToDictionary(a => a.Name, a => a.Value);

    private async Task<McpClient> ConnectAsync(WebApplicationFactory<Program> app, string username)
    {
        var http = app.CreateClient();
        // Tokens come from the shared Keycloak container, whichever host serves the API.
        http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", await factory.GetTokenAsync(username));
        var transport = new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = new Uri(http.BaseAddress!, "/api/mcp"),
                TransportMode = HttpTransportMode.StreamableHttp,
            },
            http,
            NullLoggerFactory.Instance,
            ownsHttpClient: true);
        return await McpClient.CreateAsync(transport);
    }

    private static string Text(CallToolResult result) =>
        string.Concat(result.Content.OfType<TextContentBlock>().Select(c => c.Text));

    private static JsonElement Json(CallToolResult result)
    {
        result.IsError.Should().NotBe(true, Text(result));
        return JsonSerializer.Deserialize<JsonElement>(Text(result), JsonOptions);
    }
}
