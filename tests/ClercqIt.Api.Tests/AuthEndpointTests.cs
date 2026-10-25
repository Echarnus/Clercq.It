using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

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
}
