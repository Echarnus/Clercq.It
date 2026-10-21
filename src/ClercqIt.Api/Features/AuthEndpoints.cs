using Clercq.It.Api.Features.Auth;

namespace Clercq.It.Api.Features;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/auth")
            .WithTags("Authentication")
            .WithOpenApi()
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

        // No registration or social login endpoints: the owner creates admin
        // users in the Cloud IAM console (see docs/ciam.md).

        return endpoints;
    }
}
