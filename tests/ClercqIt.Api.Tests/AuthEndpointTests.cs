using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace ClercqIt.Api.Tests;

[Collection(ApiTestGroup.Name)]
public class AuthEndpointTests : IAsyncLifetime
{
    private readonly HttpClient _client;

    public AuthEndpointTests(CustomWebApplicationFactory factory)
    {
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
