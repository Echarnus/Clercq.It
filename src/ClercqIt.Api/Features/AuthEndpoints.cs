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

        group.MapPost("/token", async (
            [FromBody] LoginRequest request,
            ITokenService tokenService) =>
        {
            // Validate Scaleway credentials with optional TOTP
            var validationResult = await tokenService.ValidateScalewayCredentials(
                request.AccessKey,
                request.SecretKey,
                request.TotpCode
            );

            // If MFA is required, return a specific response
            if (validationResult.RequiresMfa)
            {
                return Results.Json(
                    new { requiresMfa = true, message = validationResult.ErrorMessage ?? "MFA required" },
                    statusCode: 428 // 428 Precondition Required
                );
            }

            if (!validationResult.IsValid)
            {
                return Results.Json(
                    new { message = validationResult.ErrorMessage ?? "Invalid credentials" },
                    statusCode: 401
                );
            }

            // Generate JWT token
            var token = tokenService.GenerateToken(
                request.AccessKey,
                $"{request.AccessKey}@scaleway"
            );

            var expirationMinutes = 60; // Default, could be from config
            
            return Results.Ok(new
            {
                token,
                expiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes)
            });
        })
        .WithName("GenerateToken")
        .WithSummary("Generate JWT token")
        .WithDescription("Authenticates using Scaleway IAM credentials and returns a JWT token")
        .AllowAnonymous();
    }

    public record LoginRequest(string AccessKey, string SecretKey, string? TotpCode = null);
}
