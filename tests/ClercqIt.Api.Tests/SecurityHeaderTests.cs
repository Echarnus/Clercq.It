using System.Net;
using FluentAssertions;

namespace ClercqIt.Api.Tests;

public class SecurityHeaderTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SecurityHeaderTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact(DisplayName = "Response contains X-Content-Type-Options security header")]
    public async Task Response_AnyEndpoint_ContainsXContentTypeOptions()
    {
        var response = await _client.GetAsync("/health");

        response.Headers.Should().ContainKey("X-Content-Type-Options");
        response.Headers.GetValues("X-Content-Type-Options").Should().Contain("nosniff");
    }

    [Fact(DisplayName = "Response contains X-Frame-Options security header")]
    public async Task Response_AnyEndpoint_ContainsXFrameOptions()
    {
        var response = await _client.GetAsync("/health");

        response.Headers.Should().ContainKey("X-Frame-Options");
        response.Headers.GetValues("X-Frame-Options").Should().Contain("DENY");
    }

    [Fact(DisplayName = "Response contains X-XSS-Protection security header")]
    public async Task Response_AnyEndpoint_ContainsXXssProtection()
    {
        var response = await _client.GetAsync("/health");

        response.Headers.Should().ContainKey("X-XSS-Protection");
        response.Headers.GetValues("X-XSS-Protection").Should().Contain("1; mode=block");
    }

    [Fact(DisplayName = "Response contains Referrer-Policy security header")]
    public async Task Response_AnyEndpoint_ContainsReferrerPolicy()
    {
        var response = await _client.GetAsync("/health");

        response.Headers.Should().ContainKey("Referrer-Policy");
        response.Headers.GetValues("Referrer-Policy").Should().Contain("strict-origin-when-cross-origin");
    }

    [Fact(DisplayName = "Response contains Permissions-Policy security header")]
    public async Task Response_AnyEndpoint_ContainsPermissionsPolicy()
    {
        var response = await _client.GetAsync("/health");

        response.Headers.Should().ContainKey("Permissions-Policy");
        response.Headers.GetValues("Permissions-Policy").Should().Contain("camera=(), microphone=(), geolocation=()");
    }

    [Fact(DisplayName = "Response contains Content-Security-Policy header")]
    public async Task Response_AnyEndpoint_ContainsContentSecurityPolicy()
    {
        var response = await _client.GetAsync("/health");

        response.Headers.Should().ContainKey("Content-Security-Policy");
        response.Headers.GetValues("Content-Security-Policy")
            .Should().Contain("default-src 'none'; frame-ancestors 'none'");
    }

    [Fact(DisplayName = "Response does not contain Server header")]
    public async Task Response_AnyEndpoint_DoesNotContainServerHeader()
    {
        var response = await _client.GetAsync("/health");

        response.Headers.Should().NotContainKey("X-Powered-By");
    }
}
