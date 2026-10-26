using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ClercqIt.Api.Tests;

[Collection(ApiTestGroup.Name)]
public class AuthEndpointTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "POST /api/auth/register does not exist, so nobody can self-register")]
    public async Task Register_Anonymous_ReturnsNotFound()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            username = "attacker",
            email = "attacker@example.com",
            password = "Password123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "POST /api/auth/login does not exist, so no endpoint accepts a password")]
    public async Task Login_Password_ReturnsNotFoundOrMethodNotAllowed()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { username = "admin", password = "admin" });

        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed);
    }

    [Fact(DisplayName = "GET /api/auth/config returns the issuer and public client for the PKCE sign-in")]
    public async Task Config_Anonymous_ReturnsAuthorityAndClient()
    {
        var config = await _client.GetFromJsonAsync<AuthConfig>("/api/auth/config");

        config!.Authority.Should().Be($"{_factory.KeycloakUrl}/realms/{CustomWebApplicationFactory.Realm}");
        config.ClientId.Should().Be(CustomWebApplicationFactory.WebClientId);
    }

    private sealed record AuthConfig(string Authority, string ClientId);

    [Fact(DisplayName = "GET /api/auth/session without a token returns 401")]
    public async Task Session_Anonymous_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/auth/session");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "GET /api/auth/session for a user without an admin role returns 403")]
    public async Task Session_UserWithoutRoles_ReturnsForbidden()
    {
        var client = await _factory.CreateClientForUserAsync("test");

        var response = await client.GetAsync("/api/auth/session");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory(DisplayName = "GET /api/auth/session returns 204 for any admin role")]
    [InlineData("admin")]
    [InlineData("blogger")]
    [InlineData("projects")]
    [InlineData("certs")]
    public async Task Session_AdminRole_ReturnsNoContent(string username)
    {
        var client = await _factory.CreateClientForUserAsync(username);

        var response = await client.GetAsync("/api/auth/session");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact(DisplayName = "GET /api/auth/identity-provider without a token returns 401")]
    public async Task IdentityProvider_Anonymous_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/auth/identity-provider");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "GET /api/auth/identity-provider without Admin.View returns 403")]
    public async Task IdentityProvider_WithoutAdminView_ReturnsForbidden()
    {
        var client = await _factory.CreateClientForUserAsync("test");

        var response = await client.GetAsync("/api/auth/identity-provider");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "GET /api/auth/identity-provider shows the realm, console links, clients and roles to an admin")]
    public async Task IdentityProvider_Admin_ReturnsSetup()
    {
        var client = await _factory.CreateClientForUserAsync("admin");

        var response = await client.GetAsync("/api/auth/identity-provider");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        var setup = await response.Content.ReadFromJsonAsync<IdentityProvider>();
        setup!.Realm.Should().Be(CustomWebApplicationFactory.Realm);
        setup.Authority.Should().Be($"{_factory.KeycloakUrl}/realms/{CustomWebApplicationFactory.Realm}");
        setup.AdminConsoleUrl.Should().Be($"{_factory.KeycloakUrl}/admin/{CustomWebApplicationFactory.Realm}/console/");
        setup.WebClientId.Should().Be(CustomWebApplicationFactory.WebClientId);
        setup.McpClientId.Should().Be("clercqit-mcp");
        setup.Roles.Select(r => r.Name).Should().Contain(["Admin.View", "Blogs.Contributor", "MyZen.Owner"]);
        body.Should().NotContainAny("secret", "password", "Secret", "Password");
    }

    private sealed record IdentityProvider(
        string Realm, string Authority, string AdminConsoleUrl, string WebClientId, string McpClientId, List<Role> Roles);

    private sealed record Role(string Name, string Description);

    [Theory(DisplayName = "Social login endpoints do not exist")]
    [InlineData("/api/auth/github")]
    [InlineData("/api/auth/github/callback?code=x&state=y")]
    [InlineData("/api/auth/linkedin")]
    [InlineData("/api/auth/linkedin/callback?code=x&state=y")]
    public async Task SocialLogin_Anonymous_ReturnsNotFound(string url)
    {
        var response = await _client.GetAsync(url);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // The web server checks the admin session on every dashboard request from localhost,
    // which shares one rate-limit bucket with the whole site (#438). A 429 there sent
    // signed-in admins back to the sign-in page (issue #475).
    [Theory(DisplayName = "The admin sign-in endpoints are not rate limited by the API")]
    [InlineData("/api/auth/session")]
    [InlineData("/api/auth/config")]
    public void SignInEndpoints_DisableRateLimiting(string route)
    {
        var endpoint = _factory.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Single(e => "/" + e.RoutePattern.RawText?.TrimStart('/') == route
                && e.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods.Contains("GET") == true);

        endpoint.Metadata.GetMetadata<DisableRateLimitingAttribute>().Should().NotBeNull();
    }
}
