using System.Net;
using FluentAssertions;

namespace ClercqIt.Api.Tests;

[Collection(ApiTestGroup.Name)]
public class HealthCheckTests : IAsyncLifetime
{
    private readonly HttpClient _client;

    public HealthCheckTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => Task.CompletedTask;

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
