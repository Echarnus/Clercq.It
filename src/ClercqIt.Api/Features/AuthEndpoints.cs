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

        // No registration or social login endpoints: the owner creates admin
        // users in the Cloud IAM console (see docs/ciam.md).

        return endpoints;
    }

    public record LoginRequest(string Username, string Password, string? TotpCode = null);
}
