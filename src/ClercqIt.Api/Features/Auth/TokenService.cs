using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Clercq.It.Api.Features.Auth;

public interface ITokenService
{
    string GenerateToken(string userId, string email);
    Task<bool> ValidateScalewayCredentials(string accessKey, string secretKey);
}

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<TokenService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public TokenService(
        IConfiguration configuration, 
        ILogger<TokenService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
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

    public async Task<bool> ValidateScalewayCredentials(string accessKey, string secretKey)
    {
        try
        {
            // Validate credentials by making a request to Scaleway IAM API
            // We'll use the IAM API to list the current user's information
            // This endpoint requires valid credentials and will return 403 if invalid
            
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Auth-Token", secretKey);
            
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.scaleway.com/iam/v1alpha1/api-keys");
            request.Headers.Add("X-Auth-Token", secretKey);
            
            var response = await client.SendAsync(request);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully validated Scaleway IAM credentials");
                return true;
            }
            
            _logger.LogWarning("Failed to validate Scaleway credentials. Status: {StatusCode}", response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Scaleway credentials");
            return false;
        }
    }
}
