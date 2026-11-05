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
/// The MyZen invoice tools on the MCP server (issue #494), with the real MCP client and
/// real Keycloak tokens, and a mocked gateway so MyZen itself is never called.
/// </summary>
[Collection(ApiTestGroup.Name)]
public class MyZenInvoiceToolsTests(CustomWebApplicationFactory factory) : IAsyncLifetime
{
    private static readonly string[] ToolNames = ["list_myzen_invoices", "get_myzen_invoice"];

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly Mock<IMyZenInvoiceGateway> _gateway = new();
    private WebApplicationFactory<Program>? _app;

    public Task InitializeAsync()
    {
        _app = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("FeatureFlags:MyZen", "true");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IMyZenInvoiceGateway>();
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

    [Fact(DisplayName = "The MyZen owner sees the invoice tools, marked read-only")]
    public async Task ListTools_Owner_SeesReadOnlyTools()
    {
        await using var mcp = await ConnectAsync(_app!, "admin");

        var tools = (await mcp.ListToolsAsync()).Where(t => ToolNames.Contains(t.Name)).ToList();

        tools.Select(t => t.Name).Should().BeEquivalentTo(ToolNames);
        tools.Should().OnlyContain(t => t.ProtocolTool.Annotations!.ReadOnlyHint == true);
    }

    [Fact(DisplayName = "A contributor without MyZen.Owner does not see or call the invoice tools")]
    public async Task Tools_Contributor_Hidden()
    {
        await using var mcp = await ConnectAsync(_app!, "projects");

        var tools = (await mcp.ListToolsAsync()).Select(t => t.Name);
        Func<Task> act = async () => await mcp.CallToolAsync("list_myzen_invoices", Args(
            ("direction", "sales"), ("from", "2026-01-01"), ("to", "2026-01-31")));

        tools.Should().NotContain(ToolNames);
        await act.Should().ThrowAsync<ModelContextProtocol.McpException>();
        _gateway.VerifyNoOtherCalls();
    }

    [Fact(DisplayName = "The invoice tools are hidden while the MyZen flag is off")]
    public async Task ListTools_FlagOff_Hidden()
    {
        await using var mcp = await ConnectAsync(factory, "admin");

        var tools = (await mcp.ListToolsAsync()).Select(t => t.Name);

        tools.Should().NotContain(ToolNames);
    }

    [Fact(DisplayName = "Listing passes the filter and applies the counterparty filter")]
    public async Task List_ReturnsFilteredInvoices()
    {
        // Arrange
        MyZenInvoiceFilter? sent = null;
        _gateway.Setup(g => g.GetInvoicesAsync(It.IsAny<MyZenInvoiceFilter>(), It.IsAny<CancellationToken>()))
            .Callback<MyZenInvoiceFilter, CancellationToken>((f, _) => sent = f)
            .ReturnsAsync([Summary(1, "Anthropic, PBC"), Summary(2, "Proton AG")]);
        await using var mcp = await ConnectAsync(_app!, "admin");

        // Act
        var invoices = Json(await mcp.CallToolAsync("list_myzen_invoices", Args(
            ("direction", "purchase"), ("from", "2026-01-01"), ("to", "2026-12-31"),
            ("status", "Paid"), ("counterparty", "anthropic"))));

        // Assert
        invoices.GetArrayLength().Should().Be(1);
        invoices[0].GetProperty("id").GetInt64().Should().Be(1);
        invoices[0].GetProperty("totalIncl").GetDecimal().Should().Be(242m);
        sent.Should().Be(new MyZenInvoiceFilter("purchase", "invoice", new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31),
            Status: "Paid", Counterparty: "anthropic"));
    }

    [Fact(DisplayName = "An invalid filter comes back as a validation error and MyZen is not asked")]
    public async Task List_Invalid_ToolError()
    {
        await using var mcp = await ConnectAsync(_app!, "admin");

        var result = await mcp.CallToolAsync("list_myzen_invoices", Args(
            ("direction", "costs"), ("from", "2024-01-01"), ("to", "2026-01-01")));

        result.IsError.Should().BeTrue();
        Text(result).Should().Contain("'direction' must be 'sales' or 'purchase'").And.Contain("must not exceed 366 days");
        _gateway.VerifyNoOtherCalls();
    }

    [Fact(DisplayName = "Getting an invoice returns it with its lines")]
    public async Task Get_ReturnsInvoice()
    {
        // Arrange
        _gateway.Setup(g => g.GetInvoiceAsync("purchase", "invoice", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MyZenInvoiceDto(1, "purchase", "invoice", "INV-1", new DateOnly(2026, 10, 1), null, null, null, null,
                "Anthropic, PBC", 9, null, "EUR", 200m, 42m, 242m, "Paid", true, new DateOnly(2026, 10, 2), "Software",
                [new MyZenInvoiceLineDto("Claude Max plan", 1m, null, 200m, 21m, null, 200m, 42m, 242m)]));
        await using var mcp = await ConnectAsync(_app!, "admin");

        // Act
        var invoice = Json(await mcp.CallToolAsync("get_myzen_invoice", Args(("direction", "purchase"), ("id", 1))));

        // Assert
        invoice.GetProperty("counterparty").GetString().Should().Be("Anthropic, PBC");
        invoice.GetProperty("lines")[0].GetProperty("description").GetString().Should().Be("Claude Max plan");
    }

    [Fact(DisplayName = "An unknown invoice and an expired session come back as tool errors")]
    public async Task Get_Errors_AreToolErrors()
    {
        // Arrange
        _gateway.Setup(g => g.GetInvoiceAsync("sales", "invoice", 2, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new MyZenNotConnectedException());
        await using var mcp = await ConnectAsync(_app!, "admin");

        // Act
        var missing = await mcp.CallToolAsync("get_myzen_invoice", Args(("direction", "sales"), ("id", 1)));
        var expired = await mcp.CallToolAsync("get_myzen_invoice", Args(("direction", "sales"), ("id", 2)));

        // Assert
        missing.IsError.Should().BeTrue();
        Text(missing).Should().EndWith("No MyZen sales invoice with id 1.");
        expired.IsError.Should().BeTrue();
        Text(expired).Should().Contain("Connect it again");
    }

    private static MyZenInvoiceSummaryDto Summary(long id, string counterparty) =>
        new(id, "purchase", "invoice", $"INV-{id}", new DateOnly(2026, 10, 1), null, null, null, counterparty,
            200m, 42m, 242m, 0m, "Paid", "Software");

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
