using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Clercq.It.Api.Features.Auth;

public interface ITokenService
{
    string GenerateToken(string userId, string email);
    bool ValidateScalewayCredentials(string accessKey, string secretKey);
}

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<TokenService> _logger;

    public TokenService(IConfiguration configuration, ILogger<TokenService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public string GenerateToken(string userId, string email)
    {
        var jwtSecretKey = _configuration["Authentication:JwtSecretKey"];
        if (string.IsNullOrEmpty(jwtSecretKey))
        {
            throw new InvalidOperationException("JWT secret key is not configured");
        }

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("role", "admin")
        };

        var expirationMinutes = int.Parse(_configuration["Authentication:ExpirationMinutes"] ?? "60");
        var token = new JwtSecurityToken(
            issuer: _configuration["Authentication:Issuer"] ?? "Clercq.It",
            audience: _configuration["Authentication:Audience"] ?? "Clercq.It.Api",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public bool ValidateScalewayCredentials(string accessKey, string secretKey)
    {
        // In a production environment, this would validate against Scaleway IAM
        // For now, we'll validate against configured credentials
        var validAccessKey = _configuration["Scaleway:AdminAccessKey"];
        var validSecretKey = _configuration["Scaleway:AdminSecretKey"];

        if (string.IsNullOrEmpty(validAccessKey) || string.IsNullOrEmpty(validSecretKey))
        {
            _logger.LogWarning("Scaleway admin credentials are not configured");
            return false;
        }

        // Simple comparison for now
        // In production, use Scaleway SDK for proper IAM validation
        var isValid = accessKey == validAccessKey && secretKey == validSecretKey;
        
        if (!isValid)
        {
            _logger.LogWarning("Invalid Scaleway credentials provided");
        }

        return isValid;
    }
}
