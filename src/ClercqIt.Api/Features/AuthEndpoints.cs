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
            IQuasrAuthService quasrAuthService,
            ITokenService tokenService) =>
        {
            var authResult = await quasrAuthService.AuthenticateAsync(
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

            // Generate JWT token with user roles
            var token = tokenService.GenerateToken(authResult.User);

            var expirationMinutes = 60; // Default, could be from config
            
            return Results.Ok(new
            {
                token,
                user = new
                {
                    authResult.User.Id,
                    authResult.User.Username,
                    authResult.User.Email,
                    authResult.User.Roles
                },
                expiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes)
            });
        })
        .WithName("Login")
        .WithSummary("Login with username and password")
        .WithDescription("Authenticates using Quasr.io and returns a JWT token with user roles")
        .AllowAnonymous();

        // User Registration
        group.MapPost("/register", async (
            [FromBody] RegisterRequest request,
            IQuasrAuthService quasrAuthService) =>
        {
            var result = await quasrAuthService.RegisterUserAsync(
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
            var quasrApiUrl = configuration["Quasr:ApiUrl"];
            var clientRedirectUrl = configuration["Quasr:ClientRedirectUrl"] ?? "http://localhost:3000";
            
            // Redirect to Quasr.io OAuth endpoint which handles the GitHub OAuth flow
            var oauthUrl = $"{quasrApiUrl}/v1/auth/oauth/github?redirect_uri={Uri.EscapeDataString(clientRedirectUrl)}/admin/auth/callback";
            
            return Results.Redirect(oauthUrl);
        })
        .WithName("GitHubLogin")
        .WithSummary("Initiate GitHub OAuth login")
        .WithDescription("Redirects to Quasr.io GitHub OAuth flow")
        .AllowAnonymous();

        // GitHub OAuth Callback
        group.MapGet("/github/callback", async (
            [FromQuery] string code,
            [FromQuery] string state,
            IQuasrAuthService quasrAuthService,
            ITokenService tokenService,
            IConfiguration configuration) =>
        {
            var authResult = await quasrAuthService.ValidateOAuthCallbackAsync("github", code, state);

            if (!authResult.Success || authResult.User == null)
            {
                var clientRedirectUrl = configuration["Quasr:ClientRedirectUrl"] ?? "http://localhost:3000";
                return Results.Redirect($"{clientRedirectUrl}/admin?error={Uri.EscapeDataString(authResult.ErrorMessage ?? "OAuth authentication failed")}");
            }

            // Generate JWT token
            var token = tokenService.GenerateToken(authResult.User);
            
            var clientRedirectUrl2 = configuration["Quasr:ClientRedirectUrl"] ?? "http://localhost:3000";
            return Results.Redirect($"{clientRedirectUrl2}/admin/auth/callback?token={token}");
        })
        .WithName("GitHubCallback")
        .WithSummary("Handle GitHub OAuth callback")
        .WithDescription("Processes GitHub OAuth callback from Quasr.io")
        .AllowAnonymous();

        // LinkedIn OAuth Initiation
        group.MapGet("/linkedin", (IConfiguration configuration) =>
        {
            var quasrApiUrl = configuration["Quasr:ApiUrl"];
            var clientRedirectUrl = configuration["Quasr:ClientRedirectUrl"] ?? "http://localhost:3000";
            
            // Redirect to Quasr.io OAuth endpoint which handles the LinkedIn OAuth flow
            var oauthUrl = $"{quasrApiUrl}/v1/auth/oauth/linkedin?redirect_uri={Uri.EscapeDataString(clientRedirectUrl)}/admin/auth/callback";
            
            return Results.Redirect(oauthUrl);
        })
        .WithName("LinkedInLogin")
        .WithSummary("Initiate LinkedIn OAuth login")
        .WithDescription("Redirects to Quasr.io LinkedIn OAuth flow")
        .AllowAnonymous();

        // LinkedIn OAuth Callback
        group.MapGet("/linkedin/callback", async (
            [FromQuery] string code,
            [FromQuery] string state,
            IQuasrAuthService quasrAuthService,
            ITokenService tokenService,
            IConfiguration configuration) =>
        {
            var authResult = await quasrAuthService.ValidateOAuthCallbackAsync("linkedin", code, state);

            if (!authResult.Success || authResult.User == null)
            {
                var clientRedirectUrl = configuration["Quasr:ClientRedirectUrl"] ?? "http://localhost:3000";
                return Results.Redirect($"{clientRedirectUrl}/admin?error={Uri.EscapeDataString(authResult.ErrorMessage ?? "OAuth authentication failed")}");
            }

            // Generate JWT token
            var token = tokenService.GenerateToken(authResult.User);
            
            var clientRedirectUrl2 = configuration["Quasr:ClientRedirectUrl"] ?? "http://localhost:3000";
            return Results.Redirect($"{clientRedirectUrl2}/admin/auth/callback?token={token}");
        })
        .WithName("LinkedInCallback")
        .WithSummary("Handle LinkedIn OAuth callback")
        .WithDescription("Processes LinkedIn OAuth callback from Quasr.io")
        .AllowAnonymous();
    }

    public record LoginRequest(string Username, string Password, string? TotpCode = null);
    public record RegisterRequest(string Username, string Email, string Password);
}
