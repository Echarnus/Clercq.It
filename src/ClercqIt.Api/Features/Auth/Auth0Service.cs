using System.Text;
using System.Text.Json;

namespace Clercq.It.Api.Features.Auth;

public interface IAuth0Service
{
    Task<Auth0Result> AuthenticateAsync(string username, string password, string? totpCode = null);
    Task<Auth0Result> RegisterUserAsync(string username, string email, string password);
    Task<Auth0Result> ValidateOAuthCallbackAsync(string provider, string code, string state);
    Task<Auth0User?> GetUserByIdAsync(string userId);
}

public class Auth0Result
{
    public bool Success { get; set; }
    public bool RequiresMfa { get; set; }
    public string? ErrorMessage { get; set; }
    public Auth0User? User { get; set; }
    public string? EmailVerificationRequired { get; set; }
    public string? AccessToken { get; set; } // Token from Auth0
}

public class Auth0User
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public bool EmailVerified { get; set; }
    public bool MfaEnabled { get; set; }
}

public class Auth0Service : IAuth0Service
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<Auth0Service> _logger;
    private readonly string _auth0Domain;
    private readonly string _auth0ClientId;
    private readonly string _auth0ClientSecret;
    private readonly string _auth0Audience;

    public Auth0Service(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<Auth0Service> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
        _auth0Domain = configuration["Auth0:Domain"] ?? throw new InvalidOperationException("Auth0:Domain not configured");
        _auth0ClientId = configuration["Auth0:ClientId"] ?? throw new InvalidOperationException("Auth0:ClientId not configured");
        _auth0ClientSecret = configuration["Auth0:ClientSecret"] ?? throw new InvalidOperationException("Auth0:ClientSecret not configured");
        _auth0Audience = configuration["Auth0:Audience"] ?? throw new InvalidOperationException("Auth0:Audience not configured");
    }

    public async Task<Auth0Result> AuthenticateAsync(string username, string password, string? totpCode = null)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post, $"https://{_auth0Domain}/oauth/token");
            
            var payload = new
            {
                grant_type = "password",
                username,
                password,
                audience = _auth0Audience,
                client_id = _auth0ClientId,
                client_secret = _auth0ClientSecret,
                scope = "openid profile email",
                mfa_token = totpCode
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
                var result = JsonSerializer.Deserialize<Auth0LoginResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result?.AccessToken == null)
                {
                    return new Auth0Result
                    {
                        Success = false,
                        ErrorMessage = "Invalid response from authentication service"
                    };
                }

                // Get user info
                var userInfoRequest = new HttpRequestMessage(HttpMethod.Get, $"https://{_auth0Domain}/userinfo");
                userInfoRequest.Headers.Add("Authorization", $"Bearer {result.AccessToken}");
                
                var userInfoResponse = await client.SendAsync(userInfoRequest);
                var userInfoContent = await userInfoResponse.Content.ReadAsStringAsync();
                
                if (!userInfoResponse.IsSuccessStatusCode)
                {
                    return new Auth0Result
                    {
                        Success = false,
                        ErrorMessage = "Failed to retrieve user information"
                    };
                }

                var userInfo = JsonSerializer.Deserialize<Auth0UserDto>(userInfoContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return new Auth0Result
                {
                    Success = true,
                    AccessToken = result.AccessToken,
                    User = new Auth0User
                    {
                        Id = userInfo?.Sub ?? string.Empty,
                        Username = userInfo?.Nickname ?? userInfo?.Email ?? string.Empty,
                        Email = userInfo?.Email ?? string.Empty,
                        Roles = userInfo?.Roles ?? new List<string>(),
                        EmailVerified = userInfo?.EmailVerified ?? false,
                        MfaEnabled = false // Auth0 handles MFA differently
                    }
                };
            }

            // Check if MFA is required
            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden ||
                response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                var error = JsonSerializer.Deserialize<Auth0ErrorResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (error?.Error == "mfa_required" || error?.ErrorDescription?.Contains("MFA", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return new Auth0Result
                    {
                        Success = false,
                        RequiresMfa = true,
                        ErrorMessage = "MFA code required"
                    };
                }
            }

            _logger.LogWarning("Authentication failed for user {Username}. Status: {StatusCode}", username, response.StatusCode);
            
            var errorResponse = JsonSerializer.Deserialize<Auth0ErrorResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return new Auth0Result
            {
                Success = false,
                ErrorMessage = errorResponse?.ErrorDescription ?? "Authentication failed"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error authenticating with Auth0 for user {Username}", username);
            return new Auth0Result
            {
                Success = false,
                ErrorMessage = "Authentication service error"
            };
        }
    }

    public async Task<Auth0Result> RegisterUserAsync(string username, string email, string password)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post, $"https://{_auth0Domain}/dbconnections/signup");
            
            var payload = new
            {
                client_id = _auth0ClientId,
                email,
                password,
                connection = "Username-Password-Authentication",
                username,
                user_metadata = new { }
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
                var result = JsonSerializer.Deserialize<Auth0RegisterResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return new Auth0Result
                {
                    Success = true,
                    EmailVerificationRequired = "A verification email has been sent to your email address",
                    User = result != null ? new Auth0User
                    {
                        Id = result.Id ?? string.Empty,
                        Username = result.Username ?? username,
                        Email = result.Email ?? email,
                        EmailVerified = result.EmailVerified
                    } : null
                };
            }

            var errorResponse = JsonSerializer.Deserialize<Auth0ErrorResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            _logger.LogWarning("Registration failed for user {Username}. Status: {StatusCode}, Error: {Error}", 
                username, response.StatusCode, errorResponse?.ErrorDescription);

            return new Auth0Result
            {
                Success = false,
                ErrorMessage = errorResponse?.ErrorDescription ?? "Registration failed"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering user {Username} with Auth0", username);
            return new Auth0Result
            {
                Success = false,
                ErrorMessage = "Registration service error"
            };
        }
    }

    public async Task<Auth0Result> ValidateOAuthCallbackAsync(string provider, string code, string state)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post, $"https://{_auth0Domain}/oauth/token");
            
            var clientRedirectUrl = _configuration["Auth0:ClientRedirectUrl"] ?? "http://localhost:3000";
            
            var payload = new
            {
                grant_type = "authorization_code",
                client_id = _auth0ClientId,
                client_secret = _auth0ClientSecret,
                code,
                redirect_uri = $"{clientRedirectUrl}/admin/auth/callback"
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
                var result = JsonSerializer.Deserialize<Auth0OAuthResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result?.AccessToken == null)
                {
                    return new Auth0Result
                    {
                        Success = false,
                        ErrorMessage = "Invalid OAuth response"
                    };
                }

                // Get user info
                var userInfoRequest = new HttpRequestMessage(HttpMethod.Get, $"https://{_auth0Domain}/userinfo");
                userInfoRequest.Headers.Add("Authorization", $"Bearer {result.AccessToken}");
                
                var userInfoResponse = await client.SendAsync(userInfoRequest);
                var userInfoContent = await userInfoResponse.Content.ReadAsStringAsync();
                
                if (!userInfoResponse.IsSuccessStatusCode)
                {
                    return new Auth0Result
                    {
                        Success = false,
                        ErrorMessage = "Failed to retrieve user information"
                    };
                }

                var userInfo = JsonSerializer.Deserialize<Auth0UserDto>(userInfoContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return new Auth0Result
                {
                    Success = true,
                    AccessToken = result.AccessToken,
                    User = new Auth0User
                    {
                        Id = userInfo?.Sub ?? string.Empty,
                        Username = userInfo?.Nickname ?? userInfo?.Email ?? string.Empty,
                        Email = userInfo?.Email ?? string.Empty,
                        Roles = userInfo?.Roles ?? new List<string>(),
                        EmailVerified = userInfo?.EmailVerified ?? false
                    }
                };
            }

            var errorResponse = JsonSerializer.Deserialize<Auth0ErrorResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            _logger.LogWarning("OAuth validation failed for provider {Provider}. Status: {StatusCode}", 
                provider, response.StatusCode);

            return new Auth0Result
            {
                Success = false,
                ErrorMessage = errorResponse?.ErrorDescription ?? "OAuth authentication failed"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating OAuth callback for provider {Provider}", provider);
            return new Auth0Result
            {
                Success = false,
                ErrorMessage = "OAuth service error"
            };
        }
    }

    public async Task<Auth0User?> GetUserByIdAsync(string userId)
    {
        try
        {
            // Get management API token
            var managementToken = await GetManagementApiTokenAsync();
            if (string.IsNullOrEmpty(managementToken))
            {
                _logger.LogError("Failed to get management API token");
                return null;
            }

            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://{_auth0Domain}/api/v2/users/{Uri.EscapeDataString(userId)}");
            
            request.Headers.Add("Authorization", $"Bearer {managementToken}");

            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<Auth0UserDto>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result == null) return null;

                return new Auth0User
                {
                    Id = result.Sub ?? result.UserId ?? string.Empty,
                    Username = result.Nickname ?? result.Email ?? string.Empty,
                    Email = result.Email ?? string.Empty,
                    Roles = result.Roles ?? new List<string>(),
                    EmailVerified = result.EmailVerified,
                    MfaEnabled = false
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user {UserId} from Auth0", userId);
            return null;
        }
    }

    private async Task<string?> GetManagementApiTokenAsync()
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post, $"https://{_auth0Domain}/oauth/token");
            
            var payload = new
            {
                grant_type = "client_credentials",
                client_id = _auth0ClientId,
                client_secret = _auth0ClientSecret,
                audience = $"https://{_auth0Domain}/api/v2/"
            };
            
            request.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            );

            var response = await client.SendAsync(request);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<Auth0TokenResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                return result?.AccessToken;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting management API token");
            return null;
        }
    }

    // Internal response DTOs
    private class Auth0LoginResponse
    {
        public string? AccessToken { get; set; }
        public string? IdToken { get; set; }
        public string? TokenType { get; set; }
    }

    private class Auth0RegisterResponse
    {
        public string? Id { get; set; }
        public string? Email { get; set; }
        public string? Username { get; set; }
        public bool EmailVerified { get; set; }
    }

    private class Auth0OAuthResponse
    {
        public string? AccessToken { get; set; }
        public string? IdToken { get; set; }
        public string? TokenType { get; set; }
    }

    private class Auth0TokenResponse
    {
        public string? AccessToken { get; set; }
        public string? TokenType { get; set; }
    }

    private class Auth0UserDto
    {
        public string? Sub { get; set; }
        public string? UserId { get; set; }
        public string? Nickname { get; set; }
        public string? Email { get; set; }
        public List<string>? Roles { get; set; }
        public bool EmailVerified { get; set; }
    }

    private class Auth0ErrorResponse
    {
        public string? Error { get; set; }
        public string? ErrorDescription { get; set; }
    }
}
