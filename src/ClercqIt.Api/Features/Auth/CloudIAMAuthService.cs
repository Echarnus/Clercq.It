using System.Text;
using System.Text.Json;

namespace Clercq.It.Api.Features.Auth;

public interface ICloudIAMAuthService
{
    Task<CloudIAMAuthResult> AuthenticateAsync(string username, string password, string? totpCode = null);
    Task<CloudIAMAuthResult> RegisterUserAsync(string username, string email, string password);
    Task<CloudIAMAuthResult> ValidateOAuthCallbackAsync(string provider, string code, string state);
    Task<CloudIAMUser?> GetUserByIdAsync(string userId);
}

public class CloudIAMAuthResult
{
    public bool Success { get; set; }
    public bool RequiresMfa { get; set; }
    public string? ErrorMessage { get; set; }
    public CloudIAMUser? User { get; set; }
    public string? EmailVerificationRequired { get; set; }
    public string? AccessToken { get; set; } // Token from Cloud IAM
}

public class CloudIAMUser
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public bool EmailVerified { get; set; }
    public bool MfaEnabled { get; set; }
}

public class CloudIAMAuthService : ICloudIAMAuthService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CloudIAMAuthService> _logger;
    private readonly string _cloudIAMApiUrl;
    private readonly string _cloudIAMApiKey;

    public CloudIAMAuthService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<CloudIAMAuthService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
        _cloudIAMApiUrl = configuration["CloudIAM:ApiUrl"] ?? throw new InvalidOperationException("CloudIAM:ApiUrl not configured");
        _cloudIAMApiKey = configuration["CloudIAM:ApiKey"] ?? throw new InvalidOperationException("CloudIAM:ApiKey not configured");
    }

    public async Task<CloudIAMAuthResult> AuthenticateAsync(string username, string password, string? totpCode = null)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_cloudIAMApiUrl}/v1/auth/login");
            
            request.Headers.Add("Authorization", $"Bearer {_cloudIAMApiKey}");
            request.Headers.Add("X-API-Key", _cloudIAMApiKey);
            
            var payload = new
            {
                username,
                password,
                totp_code = totpCode
            };
            
            request.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            );

            var response = await client.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<CloudIAMLoginResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result?.User == null)
                {
                    return new CloudIAMAuthResult
                    {
                        Success = false,
                        ErrorMessage = "Invalid response from authentication service"
                    };
                }

                return new CloudIAMAuthResult
                {
                    Success = true,
                    AccessToken = result.AccessToken ?? result.Token, // Use token from Cloud IAM
                    User = new CloudIAMUser
                    {
                        Id = result.User.Id,
                        Username = result.User.Username,
                        Email = result.User.Email,
                        Roles = result.User.Roles ?? new List<string>(),
                        EmailVerified = result.User.EmailVerified,
                        MfaEnabled = result.User.MfaEnabled
                    }
                };
            }

            // Check if MFA is required
            if (response.StatusCode == System.Net.HttpStatusCode.PreconditionRequired ||
                response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                var error = JsonSerializer.Deserialize<CloudIAMErrorResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (error?.Code == "MFA_REQUIRED" || error?.Message?.Contains("MFA", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return new CloudIAMAuthResult
                    {
                        Success = false,
                        RequiresMfa = true,
                        ErrorMessage = "MFA code required"
                    };
                }
            }

            _logger.LogWarning("Authentication failed for user {Username}. Status: {StatusCode}", username, response.StatusCode);
            
            var errorResponse = JsonSerializer.Deserialize<CloudIAMErrorResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return new CloudIAMAuthResult
            {
                Success = false,
                ErrorMessage = errorResponse?.Message ?? "Authentication failed"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error authenticating with Cloud IAM for user {Username}", username);
            return new CloudIAMAuthResult
            {
                Success = false,
                ErrorMessage = "Authentication service error"
            };
        }
    }

    public async Task<CloudIAMAuthResult> RegisterUserAsync(string username, string email, string password)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_cloudIAMApiUrl}/v1/auth/register");
            
            request.Headers.Add("Authorization", $"Bearer {_cloudIAMApiKey}");
            request.Headers.Add("X-API-Key", _cloudIAMApiKey);
            
            var payload = new
            {
                username,
                email,
                password,
                send_verification_email = true
            };
            
            request.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            );

            var response = await client.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<CloudIAMRegisterResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return new CloudIAMAuthResult
                {
                    Success = true,
                    EmailVerificationRequired = "A verification email has been sent to your email address",
                    User = result?.User != null ? new CloudIAMUser
                    {
                        Id = result.User.Id,
                        Username = result.User.Username,
                        Email = result.User.Email,
                        EmailVerified = false
                    } : null
                };
            }

            var errorResponse = JsonSerializer.Deserialize<CloudIAMErrorResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            _logger.LogWarning("Registration failed for user {Username}. Status: {StatusCode}, Error: {Error}", 
                username, response.StatusCode, errorResponse?.Message);

            return new CloudIAMAuthResult
            {
                Success = false,
                ErrorMessage = errorResponse?.Message ?? "Registration failed"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering user {Username} with Cloud IAM", username);
            return new CloudIAMAuthResult
            {
                Success = false,
                ErrorMessage = "Registration service error"
            };
        }
    }

    public async Task<CloudIAMAuthResult> ValidateOAuthCallbackAsync(string provider, string code, string state)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_cloudIAMApiUrl}/v1/auth/oauth/callback");
            
            request.Headers.Add("Authorization", $"Bearer {_cloudIAMApiKey}");
            request.Headers.Add("X-API-Key", _cloudIAMApiKey);
            
            var payload = new
            {
                provider,
                code,
                state
            };
            
            request.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            );

            var response = await client.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<CloudIAMOAuthResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result?.User == null)
                {
                    return new CloudIAMAuthResult
                    {
                        Success = false,
                        ErrorMessage = "Invalid OAuth response"
                    };
                }

                return new CloudIAMAuthResult
                {
                    Success = true,
                    AccessToken = result.AccessToken ?? result.Token, // Use token from Cloud IAM
                    User = new CloudIAMUser
                    {
                        Id = result.User.Id,
                        Username = result.User.Username,
                        Email = result.User.Email,
                        Roles = result.User.Roles ?? new List<string>(),
                        EmailVerified = result.User.EmailVerified
                    }
                };
            }

            var errorResponse = JsonSerializer.Deserialize<CloudIAMErrorResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            _logger.LogWarning("OAuth validation failed for provider {Provider}. Status: {StatusCode}", 
                provider, response.StatusCode);

            return new CloudIAMAuthResult
            {
                Success = false,
                ErrorMessage = errorResponse?.Message ?? "OAuth authentication failed"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating OAuth callback for provider {Provider}", provider);
            return new CloudIAMAuthResult
            {
                Success = false,
                ErrorMessage = "OAuth service error"
            };
        }
    }

    public async Task<CloudIAMUser?> GetUserByIdAsync(string userId)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Get, $"{_cloudIAMApiUrl}/v1/users/{userId}");
            
            request.Headers.Add("Authorization", $"Bearer {_cloudIAMApiKey}");
            request.Headers.Add("X-API-Key", _cloudIAMApiKey);

            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<CloudIAMUserResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result?.User == null) return null;

                return new CloudIAMUser
                {
                    Id = result.User.Id,
                    Username = result.User.Username,
                    Email = result.User.Email,
                    Roles = result.User.Roles ?? new List<string>(),
                    EmailVerified = result.User.EmailVerified,
                    MfaEnabled = result.User.MfaEnabled
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user {UserId} from Cloud IAM", userId);
            return null;
        }
    }

    // Internal response DTOs
    private class CloudIAMLoginResponse
    {
        public CloudIAMUserDto? User { get; set; }
        public string? AccessToken { get; set; }
        public string? Token { get; set; }
    }

    private class CloudIAMRegisterResponse
    {
        public CloudIAMUserDto? User { get; set; }
        public bool EmailSent { get; set; }
    }

    private class CloudIAMOAuthResponse
    {
        public CloudIAMUserDto? User { get; set; }
        public string? AccessToken { get; set; }
        public string? Token { get; set; }
    }

    private class CloudIAMUserResponse
    {
        public CloudIAMUserDto? User { get; set; }
    }

    private class CloudIAMUserDto
    {
        public string Id { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<string>? Roles { get; set; }
        public bool EmailVerified { get; set; }
        public bool MfaEnabled { get; set; }
    }

    private class CloudIAMErrorResponse
    {
        public string? Code { get; set; }
        public string? Message { get; set; }
    }
}
