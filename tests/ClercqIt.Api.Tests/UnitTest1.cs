using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;

namespace ClercqIt.Api.Tests;

public class ApiEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ApiEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task HealthCheck_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AliveCheck_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/alive");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task WeatherForecast_EndpointDoesNotExist()
    {
        // Act
        var response = await _client.GetAsync("/weatherforecast");

        // Assert - Should return 404 Not Found since we removed it
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // NOTE: The following tests require database connectivity and are disabled 
    // until proper in-memory database setup can be configured for integration testing.
    // The core business logic is thoroughly tested in the Application and Domain layer unit tests.

    [Fact(Skip = "Database-dependent integration test - requires proper test database setup")]
    public async Task GetProjects_ReturnsSuccessStatusCodeWithData()
    {
        var response = await _client.GetAsync("/api/projects");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact(Skip = "Database-dependent integration test - requires proper test database setup")]
    public async Task GetFeaturedProjects_ReturnsSuccessStatusCodeWithFeaturedData()
    {
        var response = await _client.GetAsync("/api/projects/featured");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact(Skip = "Database-dependent integration test - requires proper test database setup")]
    public async Task GetBlogs_ReturnsSuccessStatusCodeWithData()
    {
        var response = await _client.GetAsync("/api/blogs");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AuthLogin_WithoutCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var content = new StringContent(
            "{\"username\":\"\",\"password\":\"\"}",
            System.Text.Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PostAsync("/api/auth/login", content);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuthLogin_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var content = new StringContent(
            "{\"username\":\"invalid_user\",\"password\":\"invalid_password\"}",
            System.Text.Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PostAsync("/api/auth/login", content);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}