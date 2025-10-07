using System.Text;
using System.Text.Json;

namespace Clercq.It.Api.Features.Auth;

public interface IQuasrAuthService
{
    Task<QuasrAuthResult> AuthenticateAsync(string username, string password, string? totpCode = null);
    Task<QuasrAuthResult> RegisterUserAsync(string username, string email, string password);
    Task<QuasrAuthResult> ValidateOAuthCallbackAsync(string provider, string code, string state);
    Task<QuasrUser?> GetUserByIdAsync(string userId);
    string GetApiUrl();
}

public class QuasrAuthResult
{
    public bool Success { get; set; }
    public bool RequiresMfa { get; set; }
    public string? ErrorMessage { get; set; }
    public QuasrUser? User { get; set; }
    public string? EmailVerificationRequired { get; set; }
    public string? AccessToken { get; set; } // Token from Quasr.io
}

public class QuasrUser
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public bool EmailVerified { get; set; }
    public bool MfaEnabled { get; set; }
}

public class QuasrAuthService : IQuasrAuthService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<QuasrAuthService> _logger;
    private readonly string _quasrApiUrl;
    private readonly string _quasrApiKey;

    public QuasrAuthService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<QuasrAuthService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
        
        // Build tenant-specific URL: https://{tenantId}.api.quasr.io
        var tenantId = configuration["Quasr:TenantId"] ?? throw new InvalidOperationException("Quasr:TenantId not configured");
        _quasrApiUrl = $"https://{tenantId}.api.quasr.io";
        _quasrApiKey = configuration["Quasr:ApiKey"] ?? throw new InvalidOperationException("Quasr:ApiKey not configured");
    }

    public string GetApiUrl() => _quasrApiUrl;

    public async Task<QuasrAuthResult> AuthenticateAsync(string username, string password, string? totpCode = null)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_quasrApiUrl}/v1/auth/login");
            
            request.Headers.Add("Authorization", $"Bearer {_quasrApiKey}");
            request.Headers.Add("X-API-Key", _quasrApiKey);
            
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
                var result = JsonSerializer.Deserialize<QuasrLoginResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result?.User == null)
                {
                    return new QuasrAuthResult
                    {
                        Success = false,
                        ErrorMessage = "Invalid response from authentication service"
                    };
                }

                return new QuasrAuthResult
                {
                    Success = true,
                    AccessToken = result.AccessToken ?? result.Token, // Use token from Quasr.io
                    User = new QuasrUser
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
                var error = JsonSerializer.Deserialize<QuasrErrorResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (error?.Code == "MFA_REQUIRED" || error?.Message?.Contains("MFA", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return new QuasrAuthResult
                    {
                        Success = false,
                        RequiresMfa = true,
                        ErrorMessage = "MFA code required"
                    };
                }
            }

            _logger.LogWarning("Authentication failed for user {Username}. Status: {StatusCode}", username, response.StatusCode);
            
            var errorResponse = JsonSerializer.Deserialize<QuasrErrorResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return new QuasrAuthResult
            {
                Success = false,
                ErrorMessage = errorResponse?.Message ?? "Authentication failed"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error authenticating with Quasr.io for user {Username}", username);
            return new QuasrAuthResult
            {
                Success = false,
                ErrorMessage = "Authentication service error"
            };
        }
    }

    public async Task<QuasrAuthResult> RegisterUserAsync(string username, string email, string password)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_quasrApiUrl}/v1/auth/register");
            
            request.Headers.Add("Authorization", $"Bearer {_quasrApiKey}");
            request.Headers.Add("X-API-Key", _quasrApiKey);
            
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
                var result = JsonSerializer.Deserialize<QuasrRegisterResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return new QuasrAuthResult
                {
                    Success = true,
                    EmailVerificationRequired = "A verification email has been sent to your email address",
                    User = result?.User != null ? new QuasrUser
                    {
                        Id = result.User.Id,
                        Username = result.User.Username,
                        Email = result.User.Email,
                        EmailVerified = false
                    } : null
                };
            }

            var errorResponse = JsonSerializer.Deserialize<QuasrErrorResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            _logger.LogWarning("Registration failed for user {Username}. Status: {StatusCode}, Error: {Error}", 
                username, response.StatusCode, errorResponse?.Message);

            return new QuasrAuthResult
            {
                Success = false,
                ErrorMessage = errorResponse?.Message ?? "Registration failed"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering user {Username} with Quasr.io", username);
            return new QuasrAuthResult
            {
                Success = false,
                ErrorMessage = "Registration service error"
            };
        }
    }

    public async Task<QuasrAuthResult> ValidateOAuthCallbackAsync(string provider, string code, string state)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_quasrApiUrl}/v1/auth/oauth/callback");
            
            request.Headers.Add("Authorization", $"Bearer {_quasrApiKey}");
            request.Headers.Add("X-API-Key", _quasrApiKey);
            
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
                var result = JsonSerializer.Deserialize<QuasrOAuthResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result?.User == null)
                {
                    return new QuasrAuthResult
                    {
                        Success = false,
                        ErrorMessage = "Invalid OAuth response"
                    };
                }

                return new QuasrAuthResult
                {
                    Success = true,
                    AccessToken = result.AccessToken ?? result.Token, // Use token from Quasr.io
                    User = new QuasrUser
                    {
                        Id = result.User.Id,
                        Username = result.User.Username,
                        Email = result.User.Email,
                        Roles = result.User.Roles ?? new List<string>(),
                        EmailVerified = result.User.EmailVerified
                    }
                };
            }

            var errorResponse = JsonSerializer.Deserialize<QuasrErrorResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            _logger.LogWarning("OAuth validation failed for provider {Provider}. Status: {StatusCode}", 
                provider, response.StatusCode);

            return new QuasrAuthResult
            {
                Success = false,
                ErrorMessage = errorResponse?.Message ?? "OAuth authentication failed"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating OAuth callback for provider {Provider}", provider);
            return new QuasrAuthResult
            {
                Success = false,
                ErrorMessage = "OAuth service error"
            };
        }
    }

    public async Task<QuasrUser?> GetUserByIdAsync(string userId)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Get, $"{_quasrApiUrl}/v1/users/{userId}");
            
            request.Headers.Add("Authorization", $"Bearer {_quasrApiKey}");
            request.Headers.Add("X-API-Key", _quasrApiKey);

            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<QuasrUserResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result?.User == null) return null;

                return new QuasrUser
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
            _logger.LogError(ex, "Error fetching user {UserId} from Quasr.io", userId);
            return null;
        }
    }

    // Internal response DTOs
    private class QuasrLoginResponse
    {
        public QuasrUserDto? User { get; set; }
        public string? AccessToken { get; set; }
        public string? Token { get; set; }
    }

    private class QuasrRegisterResponse
    {
        public QuasrUserDto? User { get; set; }
        public bool EmailSent { get; set; }
    }

    private class QuasrOAuthResponse
    {
        public QuasrUserDto? User { get; set; }
        public string? AccessToken { get; set; }
        public string? Token { get; set; }
    }

    private class QuasrUserResponse
    {
        public QuasrUserDto? User { get; set; }
    }

    private class QuasrUserDto
    {
        public string Id { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<string>? Roles { get; set; }
        public bool EmailVerified { get; set; }
        public bool MfaEnabled { get; set; }
    }

    private class QuasrErrorResponse
    {
        public string? Code { get; set; }
        public string? Message { get; set; }
    }
}
