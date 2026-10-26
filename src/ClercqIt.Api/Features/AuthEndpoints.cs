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
        .AllowAnonymous()
        // Static and cheap. The admin shell reads it on every load, and the shared
        // localhost bucket (#438) made sign-in fail with 429 (issue #475). Browsers
        // still pass nginx's /api limit.
        .DisableRateLimiting();

        // The web server asks this before it serves the admin dashboard (src/ClercqIt.Web/proxy.ts),
        // so visitors without an admin role never receive its page or code.
        group.MapGet("/session", () => Results.NoContent())
        .WithName("GetAdminSession")
        .WithSummary("Check an admin session")
        .WithDescription("Returns 204 when the bearer token belongs to a user with an admin role, else 401 or 403")
        .RequireAuthorization(AdminAreaPolicy)
        // proxy.ts calls this on every dashboard request and link prefetch, from localhost,
        // so a rate limit here sent signed-in admins back to the sign-in page (issue #475).
        .DisableRateLimiting();

        // The admin Settings page (issue #462): where Cloud IAM lives and how it is set up.
        // Only URLs, the realm, client ids and role names; never a password or client secret.
        group.MapGet("/identity-provider", (IdentityProviderSettings settings) => Results.Ok(settings))
        .WithName("GetIdentityProvider")
        .WithSummary("Get the identity provider setup for the admin settings")
        .WithDescription("Returns the Cloud IAM / Keycloak URLs, realm, client ids and roles the site uses. Contains no secrets")
        .RequireAuthorization("AdminView");

        // No registration or social login endpoints: the owner creates admin
        // users in the Cloud IAM console (see docs/ciam.md).

        return endpoints;
    }
}
