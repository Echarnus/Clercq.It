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
            // Validate Scaleway credentials
            var isValid = await tokenService.ValidateScalewayCredentials(
                request.AccessKey,
                request.SecretKey
            );

            if (!isValid)
            {
                return Results.Unauthorized();
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

    public record LoginRequest(string AccessKey, string SecretKey);
}
