using Clercq.It.Api.Features.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Clercq.It.Api.Features;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/auth")
            .WithTags("Authentication")
            .WithOpenApi();

        // Username/Password Login
        group.MapPost("/login", async (
            [FromBody] LoginRequest request,
            IAuth0Service auth0Service) =>
        {
            var authResult = await auth0Service.AuthenticateAsync(
                request.Username,
                request.Password,
                request.TotpCode
            );

            // If MFA is required, return a specific response
            if (authResult.RequiresMfa)
            {
                return Results.Json(
                    new { requiresMfa = true, message = authResult.ErrorMessage ?? "MFA required" },
                    statusCode: 428 // 428 Precondition Required
                );
            }

            if (!authResult.Success || authResult.User == null)
            {
                return Results.Json(
                    new { message = authResult.ErrorMessage ?? "Invalid credentials" },
                    statusCode: 401
                );
            }

            // Return the token from Auth0
            return Results.Ok(new
            {
                token = authResult.AccessToken,
                user = new
                {
                    authResult.User.Id,
                    authResult.User.Username,
                    authResult.User.Email,
                    authResult.User.Roles
                }
            });
        })
        .WithName("Login")
        .WithSummary("Login with username and password")
        .WithDescription("Authenticates using Auth0 and returns a JWT token from Auth0 with user roles")
        .AllowAnonymous();

        // User Registration
        group.MapPost("/register", async (
            [FromBody] RegisterRequest request,
            IAuth0Service auth0Service) =>
        {
            var result = await auth0Service.RegisterUserAsync(
                request.Username,
                request.Email,
                request.Password
            );

            if (!result.Success)
            {
                return Results.Json(
                    new { message = result.ErrorMessage ?? "Registration failed" },
                    statusCode: 400
                );
            }

            return Results.Ok(new
            {
                message = result.EmailVerificationRequired ?? "Registration successful",
                user = result.User != null ? new
                {
                    result.User.Username,
                    result.User.Email
                } : null
            });
        })
        .WithName("Register")
        .WithSummary("Register a new user")
        .WithDescription("Creates a new user account with email verification")
        .AllowAnonymous();

        // GitHub OAuth Initiation
        group.MapGet("/github", (IConfiguration configuration) =>
        {
            var auth0Domain = configuration["Auth0:Domain"];
            var auth0ClientId = configuration["Auth0:ClientId"];
            var clientRedirectUrl = configuration["Auth0:ClientRedirectUrl"] ?? "http://localhost:3000";
            
            // Redirect to Auth0 OAuth endpoint which handles the GitHub OAuth flow
            var oauthUrl = $"https://{auth0Domain}/authorize?response_type=code&client_id={auth0ClientId}&connection=github&redirect_uri={Uri.EscapeDataString(clientRedirectUrl)}/admin/auth/callback&scope=openid%20profile%20email";
            
            return Results.Redirect(oauthUrl);
        })
        .WithName("GitHubLogin")
        .WithSummary("Initiate GitHub OAuth login")
        .WithDescription("Redirects to Auth0 GitHub OAuth flow")
        .AllowAnonymous();

        // GitHub OAuth Callback
        group.MapGet("/github/callback", async (
            [FromQuery] string code,
            [FromQuery] string state,
            IAuth0Service auth0Service,
            IConfiguration configuration) =>
        {
            var authResult = await auth0Service.ValidateOAuthCallbackAsync("github", code, state);

            if (!authResult.Success || authResult.User == null)
            {
                var clientRedirectUrl = configuration["Auth0:ClientRedirectUrl"] ?? "http://localhost:3000";
                return Results.Redirect($"{clientRedirectUrl}/admin?error={Uri.EscapeDataString(authResult.ErrorMessage ?? "OAuth authentication failed")}");
            }

            // Use token from Auth0
            var token = authResult.AccessToken;
            
            var clientRedirectUrl2 = configuration["Auth0:ClientRedirectUrl"] ?? "http://localhost:3000";
            return Results.Redirect($"{clientRedirectUrl2}/admin/auth/callback?token={token}");
        })
        .WithName("GitHubCallback")
        .WithSummary("Handle GitHub OAuth callback")
        .WithDescription("Processes GitHub OAuth callback from Auth0")
        .AllowAnonymous();

        // LinkedIn OAuth Initiation
        group.MapGet("/linkedin", (IConfiguration configuration) =>
        {
            var auth0Domain = configuration["Auth0:Domain"];
            var auth0ClientId = configuration["Auth0:ClientId"];
            var clientRedirectUrl = configuration["Auth0:ClientRedirectUrl"] ?? "http://localhost:3000";
            
            // Redirect to Auth0 OAuth endpoint which handles the LinkedIn OAuth flow
            var oauthUrl = $"https://{auth0Domain}/authorize?response_type=code&client_id={auth0ClientId}&connection=linkedin&redirect_uri={Uri.EscapeDataString(clientRedirectUrl)}/admin/auth/callback&scope=openid%20profile%20email";
            
            return Results.Redirect(oauthUrl);
        })
        .WithName("LinkedInLogin")
        .WithSummary("Initiate LinkedIn OAuth login")
        .WithDescription("Redirects to Auth0 LinkedIn OAuth flow")
        .AllowAnonymous();

        // LinkedIn OAuth Callback
        group.MapGet("/linkedin/callback", async (
            [FromQuery] string code,
            [FromQuery] string state,
            IAuth0Service auth0Service,
            IConfiguration configuration) =>
        {
            var authResult = await auth0Service.ValidateOAuthCallbackAsync("linkedin", code, state);

            if (!authResult.Success || authResult.User == null)
            {
                var clientRedirectUrl = configuration["Auth0:ClientRedirectUrl"] ?? "http://localhost:3000";
                return Results.Redirect($"{clientRedirectUrl}/admin?error={Uri.EscapeDataString(authResult.ErrorMessage ?? "OAuth authentication failed")}");
            }

            // Use token from Auth0
            var token = authResult.AccessToken;
            
            var clientRedirectUrl2 = configuration["Auth0:ClientRedirectUrl"] ?? "http://localhost:3000";
            return Results.Redirect($"{clientRedirectUrl2}/admin/auth/callback?token={token}");
        })
        .WithName("LinkedInCallback")
        .WithSummary("Handle LinkedIn OAuth callback")
        .WithDescription("Processes LinkedIn OAuth callback from Auth0")
        .AllowAnonymous();
    }

    public record LoginRequest(string Username, string Password, string? TotpCode = null);
    public record RegisterRequest(string Username, string Email, string Password);
}
