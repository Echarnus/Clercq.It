using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace ClercqIt.Api.Tests;

/// <summary>
/// Generates JWT tokens for integration testing.
/// Since the Test environment has all JWT validation disabled
/// (ValidateIssuer, ValidateAudience, ValidateLifetime, ValidateIssuerSigningKey are all false),
/// these tokens only need to be structurally valid JWTs with the correct claims.
/// A SignatureValidator override is applied in the factory to accept any signature.
/// </summary>
public static class TestJwtTokenHelper
{
    private static readonly string TestSigningKey = "ThisIsATestSigningKeyThatIsLongEnoughForHmacSha256AlgorithmAndShouldBeAtLeast64Bytes!!";

    /// <summary>
    /// The symmetric security key used for signing test tokens.
    /// Must be shared with the test authentication configuration.
    /// </summary>
    public static SymmetricSecurityKey SecurityKey => new(Encoding.UTF8.GetBytes(TestSigningKey));

    /// <summary>
    /// Generates a valid JWT token with the specified roles.
    /// </summary>
    /// <param name="roles">The roles to include in the token claims.</param>
    /// <returns>A signed JWT token string.</returns>
    public static string GenerateToken(params string[] roles)
    {
        var credentials = new SigningCredentials(SecurityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, "test-user-id"),
            new(JwtRegisteredClaimNames.Email, "test@clercq.it"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("preferred_username", "testuser")
        };

        // Add each role as a separate "roles" claim (matching the RoleClaimType used by the app)
        foreach (var role in roles)
        {
            claims.Add(new Claim("roles", role));
            // Also add as standard ClaimTypes.Role for compatibility
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var token = new JwtSecurityToken(
            issuer: "test-issuer",
            audience: "test-audience",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
