namespace Clercq.It.Api.Features.Auth;

/// <summary>
/// What the admin UI needs to start the authorization code + PKCE flow (issue #257):
/// the realm's issuer URL and the public client id. Neither is secret.
/// </summary>
public sealed record KeycloakClientSettings(string Authority, string ClientId);
