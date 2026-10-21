using System.Net;
using FluentAssertions;

namespace ClercqIt.Api.Tests;

/// <summary>
/// Every endpoint that changes data rejects anonymous callers (401) and callers
/// without the matching contributor role (403), with real Keycloak tokens.
/// </summary>
[Collection(ApiTestGroup.Name)]
public class EndpointAuthorizationTests(CustomWebApplicationFactory factory)
{
    private const string AnyId = "6f0c1f2e-0000-4000-8000-000000000001";

    // Method, path, a realm user who is signed in but lacks the role for this endpoint
    public static TheoryData<string, string, string> ProtectedEndpoints => new()
    {
        { "POST", "/api/blogs", "projects" },
        { "PUT", $"/api/blogs/{AnyId}", "projects" },
        { "DELETE", $"/api/blogs/{AnyId}", "certs" },
        { "POST", "/api/images", "certs" },
        { "POST", "/api/projects", "blogger" },
        { "PUT", $"/api/projects/{AnyId}", "certs" },
        { "DELETE", $"/api/projects/{AnyId}", "blogger" },
        { "POST", "/api/certifications", "blogger" },
        { "PUT", $"/api/certifications/{AnyId}", "projects" },
        { "DELETE", $"/api/certifications/{AnyId}", "projects" },
    };

    [Theory(DisplayName = "Protected endpoint without a token returns 401")]
    [MemberData(nameof(ProtectedEndpoints))]
    public async Task ProtectedEndpoint_Anonymous_ReturnsUnauthorized(string method, string path, string _)
    {
        var response = await factory.CreateClient().SendAsync(Request(method, path));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory(DisplayName = "Protected endpoint for a user without any role returns 403")]
    [MemberData(nameof(ProtectedEndpoints))]
    public async Task ProtectedEndpoint_UserWithoutRoles_ReturnsForbidden(string method, string path, string _)
    {
        var client = await factory.CreateClientForUserAsync("test");

        var response = await client.SendAsync(Request(method, path));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory(DisplayName = "Protected endpoint for a contributor of another content type returns 403")]
    [MemberData(nameof(ProtectedEndpoints))]
    public async Task ProtectedEndpoint_WrongContributorRole_ReturnsForbidden(string method, string path, string wrongUser)
    {
        var client = await factory.CreateClientForUserAsync(wrongUser);

        var response = await client.SendAsync(Request(method, path));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "A forged token that Keycloak did not sign returns 401")]
    public async Task ProtectedEndpoint_UnsignedToken_ReturnsUnauthorized()
    {
        var real = await factory.GetTokenAsync("admin");
        var forged = string.Join('.', real.Split('.').Take(2)) + ".invalidsignature";
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", forged);

        var response = await client.SendAsync(Request("POST", "/api/blogs"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static HttpRequestMessage Request(string method, string path) =>
        new(new HttpMethod(method), path) { Content = new MultipartFormDataContent() };
}
