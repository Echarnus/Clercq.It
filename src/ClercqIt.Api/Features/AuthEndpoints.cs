using Clercq.It.Api.Features.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Clercq.It.Api.Features;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/auth")
            .WithTags("Authentication")
            .WithOpenApi()
            .RequireRateLimiting("auth");

        // Username/Password Login
        group.MapPost("/login", async (
            [FromBody] LoginRequest request,
            ICloudIAMAuthService cloudIAMAuthService) =>
        {
            var authResult = await cloudIAMAuthService.AuthenticateAsync(
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

            // Return the token from Cloud IAM (not self-generated)
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
        .WithDescription("Authenticates using Cloud IAM and returns a JWT token from Cloud IAM with user roles")
        .AllowAnonymous();

        // User Registration
        group.MapPost("/register", async (
            [FromBody] RegisterRequest request,
            ICloudIAMAuthService cloudIAMAuthService) =>
        {
            var result = await cloudIAMAuthService.RegisterUserAsync(
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
            var cloudIAMApiUrl = configuration["CloudIAM:ApiUrl"];
            var clientRedirectUrl = configuration["CloudIAM:ClientRedirectUrl"] ?? "http://localhost:3000";
            
            // Redirect to Cloud IAM OAuth endpoint which handles the GitHub OAuth flow
            var oauthUrl = $"{cloudIAMApiUrl}/v1/auth/oauth/github?redirect_uri={Uri.EscapeDataString(clientRedirectUrl)}/admin/auth/callback";
            
            return Results.Redirect(oauthUrl);
        })
        .WithName("GitHubLogin")
        .WithSummary("Initiate GitHub OAuth login")
        .WithDescription("Redirects to Cloud IAM GitHub OAuth flow")
        .AllowAnonymous();

        // GitHub OAuth Callback
        group.MapGet("/github/callback", async (
            [FromQuery] string code,
            [FromQuery] string state,
            ICloudIAMAuthService cloudIAMAuthService,
            IConfiguration configuration) =>
        {
            var authResult = await cloudIAMAuthService.ValidateOAuthCallbackAsync("github", code, state);

            if (!authResult.Success || authResult.User == null)
            {
                var clientRedirectUrl = configuration["CloudIAM:ClientRedirectUrl"] ?? "http://localhost:3000";
                return Results.Redirect($"{clientRedirectUrl}/admin?error={Uri.EscapeDataString(authResult.ErrorMessage ?? "OAuth authentication failed")}");
            }

            // Use token from Cloud IAM
            var token = authResult.AccessToken;
            
            var clientRedirectUrl2 = configuration["CloudIAM:ClientRedirectUrl"] ?? "http://localhost:3000";
            return Results.Redirect($"{clientRedirectUrl2}/admin/auth/callback?token={token}");
        })
        .WithName("GitHubCallback")
        .WithSummary("Handle GitHub OAuth callback")
        .WithDescription("Processes GitHub OAuth callback from Cloud IAM")
        .AllowAnonymous();

        // LinkedIn OAuth Initiation
        group.MapGet("/linkedin", (IConfiguration configuration) =>
        {
            var cloudIAMApiUrl = configuration["CloudIAM:ApiUrl"];
            var clientRedirectUrl = configuration["CloudIAM:ClientRedirectUrl"] ?? "http://localhost:3000";
            
            // Redirect to Cloud IAM OAuth endpoint which handles the LinkedIn OAuth flow
            var oauthUrl = $"{cloudIAMApiUrl}/v1/auth/oauth/linkedin?redirect_uri={Uri.EscapeDataString(clientRedirectUrl)}/admin/auth/callback";
            
            return Results.Redirect(oauthUrl);
        })
        .WithName("LinkedInLogin")
        .WithSummary("Initiate LinkedIn OAuth login")
        .WithDescription("Redirects to Cloud IAM LinkedIn OAuth flow")
        .AllowAnonymous();

        // LinkedIn OAuth Callback
        group.MapGet("/linkedin/callback", async (
            [FromQuery] string code,
            [FromQuery] string state,
            ICloudIAMAuthService cloudIAMAuthService,
            IConfiguration configuration) =>
        {
            var authResult = await cloudIAMAuthService.ValidateOAuthCallbackAsync("linkedin", code, state);

            if (!authResult.Success || authResult.User == null)
            {
                var clientRedirectUrl = configuration["CloudIAM:ClientRedirectUrl"] ?? "http://localhost:3000";
                return Results.Redirect($"{clientRedirectUrl}/admin?error={Uri.EscapeDataString(authResult.ErrorMessage ?? "OAuth authentication failed")}");
            }

            // Use token from Cloud IAM
            var token = authResult.AccessToken;
            
            var clientRedirectUrl2 = configuration["CloudIAM:ClientRedirectUrl"] ?? "http://localhost:3000";
            return Results.Redirect($"{clientRedirectUrl2}/admin/auth/callback?token={token}");
        })
        .WithName("LinkedInCallback")
        .WithSummary("Handle LinkedIn OAuth callback")
        .WithDescription("Processes LinkedIn OAuth callback from Cloud IAM")
        .AllowAnonymous();

        return endpoints;
    }

    public record LoginRequest(string Username, string Password, string? TotpCode = null);
    public record RegisterRequest(string Username, string Email, string Password);
}
