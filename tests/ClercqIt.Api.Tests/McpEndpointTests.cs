using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace ClercqIt.Api.Tests;

/// <summary>
/// The content MCP server (issue #386, docs/mcp.md), called with the real MCP client
/// and real Keycloak tokens: auth, the OAuth metadata, and every tool.
/// </summary>
[Collection(ApiTestGroup.Name)]
public class McpEndpointTests(CustomWebApplicationFactory factory) : IAsyncLifetime
{
    private const string McpPath = "/api/mcp";
    // A 1x1 transparent PNG
    private const string PngBase64 =
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "MCP without a token returns 401 that points at the OAuth metadata")]
    public async Task Mcp_Anonymous_ReturnsUnauthorizedWithResourceMetadata()
    {
        var response = await factory.CreateClient().PostAsync(McpPath, InitializeBody());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.WwwAuthenticate.ToString()
            .Should().Contain("resource_metadata=").And.Contain("/.well-known/oauth-protected-resource/api/mcp");
    }

    [Fact(DisplayName = "The metadata link in the 401 resolves to the metadata for the same origin")]
    public async Task Mcp_Unauthorized_MetadataLinkResolves()
    {
        // Regression: an absolute metadata URI with another scheme or host made the link a 404 (#386)
        var client = factory.CreateClient();
        var challenge = await client.PostAsync(McpPath, InitializeBody());
        var link = System.Text.RegularExpressions.Regex.Match(
            challenge.Headers.WwwAuthenticate.ToString(), "resource_metadata=\"([^\"]+)\"").Groups[1].Value;

        var response = await client.GetAsync(new Uri(link).PathAndQuery);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var metadata = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());
        metadata.GetProperty("resource").GetString()
            .Should().Be(new Uri(new Uri(link), McpPath).ToString());
    }

    [Fact(DisplayName = "OAuth protected resource metadata names Keycloak as the authorization server")]
    public async Task ResourceMetadata_ReturnsKeycloakRealm()
    {
        var response = await factory.CreateClient().GetAsync("/.well-known/oauth-protected-resource/api/mcp");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var metadata = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());
        metadata.GetProperty("resource").GetString().Should().EndWith(McpPath);
        metadata.GetProperty("authorization_servers")[0].GetString()
            .Should().Be($"{factory.KeycloakUrl}/realms/{CustomWebApplicationFactory.Realm}");
    }

    [Fact(DisplayName = "MCP with a forged token returns 401")]
    public async Task Mcp_UnsignedToken_ReturnsUnauthorized()
    {
        var real = await factory.GetTokenAsync("admin");
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new("Bearer", string.Join('.', real.Split('.').Take(2)) + ".invalidsignature");

        var response = await client.PostAsync(McpPath, InitializeBody());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "MCP for a user without a contributor role returns 403")]
    public async Task Mcp_UserWithoutRoles_ReturnsForbidden()
    {
        var client = await factory.CreateClientForUserAsync("test");

        var response = await client.PostAsync(McpPath, InitializeBody());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "A contributor only sees the tools for their own content type")]
    public async Task ListTools_ProjectsContributor_SeesOnlyProjectTools()
    {
        await using var mcp = await ConnectAsync("projects");

        var tools = (await mcp.ListToolsAsync()).Select(t => t.Name).ToList();

        tools.Should().BeEquivalentTo(
            ["list_projects", "get_project", "create_project", "update_project", "delete_project"]);
    }

    [Fact(DisplayName = "A contributor cannot call a tool for another content type")]
    public async Task CallTool_WrongContributorRole_Fails()
    {
        await using var mcp = await ConnectAsync("projects");

        Func<Task> act = async () => await mcp.CallToolAsync("list_certifications");

        await act.Should().ThrowAsync<McpException>();
    }

    [Fact(DisplayName = "Project tools create, update, read and delete a project")]
    public async Task ProjectTools_FullLifecycle()
    {
        await using var mcp = await ConnectAsync("admin");

        var created = Json(await mcp.CallToolAsync("create_project", new Dictionary<string, object?>
        {
            ["title"] = "Child Benefit modernisation",
            ["company"] = "Certia",
            ["shortDescription"] = "Migrating a WinForms app to the web.",
            ["longDescription"] = "Angular and **.NET**.",
            ["startDate"] = "2025-05-01",
            ["endDate"] = "2026-10-23",
            ["skills"] = new[] { "Angular", ".NET" },
            ["featured"] = true,
            ["imageBase64"] = PngBase64,
            ["imageFileName"] = "certia.png",
        }));
        var id = created.GetProperty("id").GetGuid();
        created.GetProperty("image").GetString().Should().Be(CustomWebApplicationFactory.FakeImageUrl);
        created.GetProperty("company").GetString().Should().Be("Certia");

        var updated = Json(await mcp.CallToolAsync("update_project", new Dictionary<string, object?>
        {
            ["id"] = id,
            ["title"] = "Child Benefit 2.0",
        }));
        updated.GetProperty("title").GetString().Should().Be("Child Benefit 2.0");
        updated.GetProperty("shortDescription").GetString().Should().Be("Migrating a WinForms app to the web.");
        updated.GetProperty("featured").GetBoolean().Should().BeTrue();
        updated.GetProperty("company").GetString().Should().Be("Certia", "fields left out keep their value");

        var cleared = Json(await mcp.CallToolAsync("update_project", new Dictionary<string, object?>
        {
            ["id"] = id,
            ["company"] = "",
        }));
        cleared.GetProperty("company").GetString().Should().BeEmpty();

        // The site reads the same row
        var site = await factory.CreateClient().GetAsync($"/api/projects/{id}");
        site.StatusCode.Should().Be(HttpStatusCode.OK);

        var list = Json(await mcp.CallToolAsync("list_projects"));
        list.EnumerateArray().Should().ContainSingle();

        (await mcp.CallToolAsync("delete_project", new Dictionary<string, object?> { ["id"] = id }))
            .IsError.Should().NotBe(true);
        (await factory.CreateClient().GetAsync($"/api/projects/{id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "A validation failure comes back as a tool error with the message")]
    public async Task CreateProject_Invalid_ReturnsToolError()
    {
        await using var mcp = await ConnectAsync("projects");

        var result = await mcp.CallToolAsync("create_project", new Dictionary<string, object?>
        {
            ["title"] = "Backwards",
            ["shortDescription"] = "Ends before it starts",
            ["longDescription"] = "Body",
            ["startDate"] = "2025-05-01",
            ["endDate"] = "2024-05-01",
            ["skills"] = new[] { "C#" },
        });

        result.IsError.Should().BeTrue();
        Text(result).Should().Contain("End date must be greater than or equal to start date");
    }

    [Fact(DisplayName = "Updating an unknown project returns a tool error")]
    public async Task UpdateProject_Unknown_ReturnsToolError()
    {
        await using var mcp = await ConnectAsync("projects");

        var result = await mcp.CallToolAsync("update_project", new Dictionary<string, object?>
        {
            ["id"] = Guid.NewGuid(),
            ["title"] = "Nothing",
        });

        result.IsError.Should().BeTrue();
        Text(result).Should().Contain("No project with id");
    }

    [Fact(DisplayName = "An image that is not jpg, png, gif or webp is refused")]
    public async Task CreateProject_SvgImage_ReturnsToolError()
    {
        await using var mcp = await ConnectAsync("projects");

        var result = await mcp.CallToolAsync("create_project", new Dictionary<string, object?>
        {
            ["title"] = "Logo",
            ["shortDescription"] = "Short",
            ["longDescription"] = "Body",
            ["startDate"] = "2024-01-01",
            ["endDate"] = "2024-02-01",
            ["skills"] = new[] { "C#" },
            ["imageBase64"] = PngBase64,
            ["imageFileName"] = "logo.svg",
        });

        result.IsError.Should().BeTrue();
        Text(result).Should().Contain("imageFileName");
    }

    [Fact(DisplayName = "Certification tools create, update and delete a certification")]
    public async Task CertificationTools_FullLifecycle()
    {
        await using var mcp = await ConnectAsync("certs");

        var created = Json(await mcp.CallToolAsync("create_certification", new Dictionary<string, object?>
        {
            ["title"] = "Microsoft Certified: Azure Developer Associate",
            ["issuer"] = "Microsoft",
            ["issueDate"] = "2023-05-01",
            ["expiryDate"] = "2027-05-01",
            ["description"] = "AZ-204",
            ["credentialUrl"] = "https://www.credly.com/badges/example",
            ["imageBase64"] = PngBase64,
            ["imageFileName"] = "az-204.png",
        }));
        var id = created.GetProperty("id").GetGuid();

        var updated = Json(await mcp.CallToolAsync("update_certification", new Dictionary<string, object?>
        {
            ["id"] = id,
            ["clearExpiryDate"] = true,
        }));
        // Tool results leave out null fields
        (updated.TryGetProperty("expiryDate", out var expiry) && expiry.ValueKind != JsonValueKind.Null)
            .Should().BeFalse();
        updated.GetProperty("issuer").GetString().Should().Be("Microsoft");

        (await mcp.CallToolAsync("delete_certification", new Dictionary<string, object?> { ["id"] = id }))
            .IsError.Should().NotBe(true);
        Json(await mcp.CallToolAsync("list_certifications")).EnumerateArray().Should().BeEmpty();
    }

    [Fact(DisplayName = "Blog tools create, update and delete a blog post")]
    public async Task BlogTools_FullLifecycle()
    {
        await using var mcp = await ConnectAsync("blogger");

        var created = Json(await mcp.CallToolAsync("create_blog_post", new Dictionary<string, object?>
        {
            ["title"] = "EU-first infrastructure",
            ["shortDescription"] = "Running clercq.it on European services.",
            ["longDescription"] = "# Why\nBecause.",
            ["tags"] = new[] { "Scaleway", "Terraform" },
            ["imageBase64"] = PngBase64,
            ["imageFileName"] = "cover.png",
        }));
        var id = created.GetProperty("id").GetGuid();

        var updated = Json(await mcp.CallToolAsync("update_blog_post", new Dictionary<string, object?>
        {
            ["id"] = id,
            ["tags"] = new[] { "Scaleway" },
        }));
        updated.GetProperty("tags").EnumerateArray().Select(t => t.GetString()).Should().Equal("Scaleway");
        updated.GetProperty("title").GetString().Should().Be("EU-first infrastructure");

        (await mcp.CallToolAsync("delete_blog_post", new Dictionary<string, object?> { ["id"] = id }))
            .IsError.Should().NotBe(true);
        (await factory.CreateClient().GetAsync($"/api/blogs/{id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "Creating a blog post without an image returns a tool error")]
    public async Task CreateBlogPost_WithoutImage_ReturnsToolError()
    {
        await using var mcp = await ConnectAsync("blogger");

        var result = await mcp.CallToolAsync("create_blog_post", new Dictionary<string, object?>
        {
            ["title"] = "No cover",
            ["shortDescription"] = "Short",
            ["longDescription"] = "Body",
            ["tags"] = new[] { "x" },
            ["imageBase64"] = "",
            ["imageFileName"] = "cover.png",
        });

        result.IsError.Should().BeTrue();
        Text(result).Should().Contain("imageBase64 is required");
    }

    private async Task<McpClient> ConnectAsync(string username)
    {
        var http = factory.CreateClient();
        http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", await factory.GetTokenAsync(username));
        var transport = new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = new Uri(http.BaseAddress!, McpPath),
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

    private static StringContent InitializeBody()
    {
        var body = new StringContent(
            """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"test","version":"1"}}}""");
        body.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        return body;
    }
}
