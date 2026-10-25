using Clercq.It.Api.Features.Auth;

namespace Clercq.It.Api.Features;

public static class AuthEndpoints
{
    /// <summary>Any of the admin roles: who may open the admin pages (issue #454, docs/application-security.md).</summary>
    public const string AdminAreaPolicy = "AdminArea";

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/auth")
            .WithTags("Authentication")
            // Public, static settings: the normal API limit. The stricter "auth"
            // policy was for the password endpoint this replaced.
            .RequireRateLimiting("api");

        // The admin UI signs in with the authorization code flow + PKCE straight
        // against Keycloak (issue #257); no endpoint here accepts a password.
        group.MapGet("/config", (KeycloakClientSettings settings) =>
            Results.Ok(new { authority = settings.Authority, clientId = settings.ClientId }))
        .WithName("GetAuthConfig")
        .WithSummary("Get the OpenID Connect settings for the admin UI")
        .WithDescription("Returns the Keycloak issuer URL and the public client id the admin UI signs in with")
        .AllowAnonymous();

        // The web server asks this before it serves the admin dashboard (src/ClercqIt.Web/proxy.ts),
        // so visitors without an admin role never receive its page or code.
        group.MapGet("/session", () => Results.NoContent())
        .WithName("GetAdminSession")
        .WithSummary("Check an admin session")
        .WithDescription("Returns 204 when the bearer token belongs to a user with an admin role, else 401 or 403")
        .RequireAuthorization(AdminAreaPolicy);

        // No registration or social login endpoints: the owner creates admin
        // users in the Cloud IAM console (see docs/ciam.md).

        return endpoints;
    }
}
