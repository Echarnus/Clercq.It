using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Clercq.It.Api.Features.Auth;

public interface ITokenService
{
    string GenerateToken(string userId, string email);
    Task<ScalewayValidationResult> ValidateScalewayCredentials(string accessKey, string secretKey, string? totpCode = null);
}

public class ScalewayValidationResult
{
    public bool IsValid { get; set; }
    public bool RequiresMfa { get; set; }
    public string? ErrorMessage { get; set; }
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

    public async Task<ScalewayValidationResult> ValidateScalewayCredentials(string accessKey, string secretKey, string? totpCode = null)
    {
        try
        {
            // Validate credentials by making a request to Scaleway IAM API
            // We'll use the IAM API to list the current user's information
            // This endpoint requires valid credentials and will return 403 if invalid
            
            var client = _httpClientFactory.CreateClient();
            
            // Add authentication headers
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.scaleway.com/iam/v1alpha1/api-keys");
            request.Headers.Add("X-Auth-Token", secretKey);
            
            // Add MFA TOTP code if provided
            if (!string.IsNullOrEmpty(totpCode))
            {
                request.Headers.Add("X-Auth-MFA", totpCode);
            }
            
            var response = await client.SendAsync(request);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully validated Scaleway IAM credentials");
                return new ScalewayValidationResult 
                { 
                    IsValid = true, 
                    RequiresMfa = false 
                };
            }
            
            // Check if MFA is required
            // Scaleway returns 401 or 403 with specific headers when MFA is needed
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || 
                response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                // Check response headers or body for MFA requirement indication
                var responseContent = await response.Content.ReadAsStringAsync();
                
                // Scaleway typically indicates MFA requirement in the response
                // Check for common MFA-related indicators
                var requiresMfa = responseContent.Contains("mfa", StringComparison.OrdinalIgnoreCase) ||
                                 responseContent.Contains("two_factor", StringComparison.OrdinalIgnoreCase) ||
                                 responseContent.Contains("totp", StringComparison.OrdinalIgnoreCase) ||
                                 response.Headers.Contains("X-MFA-Required");
                
                if (requiresMfa && string.IsNullOrEmpty(totpCode))
                {
                    _logger.LogInformation("Scaleway credentials valid but MFA is required");
                    return new ScalewayValidationResult 
                    { 
                        IsValid = false, 
                        RequiresMfa = true,
                        ErrorMessage = "MFA required"
                    };
                }
                
                // If TOTP was provided but still failed, it's invalid
                if (!string.IsNullOrEmpty(totpCode))
                {
                    _logger.LogWarning("Invalid MFA code provided");
                    return new ScalewayValidationResult 
                    { 
                        IsValid = false, 
                        RequiresMfa = true,
                        ErrorMessage = "Invalid MFA code"
                    };
                }
            }
            
            _logger.LogWarning("Failed to validate Scaleway credentials. Status: {StatusCode}", response.StatusCode);
            return new ScalewayValidationResult 
            { 
                IsValid = false, 
                RequiresMfa = false,
                ErrorMessage = "Invalid credentials"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Scaleway credentials");
            return new ScalewayValidationResult 
            { 
                IsValid = false, 
                RequiresMfa = false,
                ErrorMessage = "Validation error"
            };
        }
    }
}
