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

    [Fact]
    public async Task GetProjects_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/api/projects");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetFeaturedProjects_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/api/projects/featured");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetBlogs_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/api/blogs");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}