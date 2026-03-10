using System.Net;
using FluentAssertions;

namespace ClercqIt.Api.Tests;

public class HealthCheckTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthCheckTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact(DisplayName = "Health check endpoint returns OK")]
    public async Task HealthCheck_EndpointCalled_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact(DisplayName = "Alive check endpoint returns OK")]
    public async Task AliveCheck_EndpointCalled_ReturnsOk()
    {
        var response = await _client.GetAsync("/alive");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
