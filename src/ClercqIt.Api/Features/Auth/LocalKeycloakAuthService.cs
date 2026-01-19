using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Clercq.It.Api.Features.Auth;

/// <summary>
/// Local Keycloak implementation of ICloudIAMAuthService for development.
/// Uses Keycloak's direct access grants (Resource Owner Password Credentials) for authentication.
/// </summary>
public class LocalKeycloakAuthService : ICloudIAMAuthService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LocalKeycloakAuthService> _logger;
    private readonly string _keycloakUrl;
    private readonly string _realm;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _adminClientId;
    private readonly string _adminClientSecret;

    public LocalKeycloakAuthService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<LocalKeycloakAuthService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;

        // Get Keycloak URL from Aspire service reference (services__keycloak__http__0) or fallback to configuration
        _keycloakUrl = configuration["services:keycloak:http:0"]
            ?? configuration["Keycloak:BaseUrl"]
            ?? "http://localhost:8080";

        _realm = configuration["Keycloak:Realm"] ?? "clercqit";
        _clientId = configuration["Keycloak:ClientId"] ?? "clercqit-web";
        _clientSecret = configuration["Keycloak:ClientSecret"] ?? "clercqit-web-secret-local";
        _adminClientId = configuration["Keycloak:AdminClientId"] ?? "clercqit-admin";
        _adminClientSecret = configuration["Keycloak:AdminClientSecret"] ?? "clercqit-admin-secret-local";
    }

    public async Task<CloudIAMAuthResult> AuthenticateAsync(string username, string password, string? totpCode = null)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var tokenEndpoint = $"{_keycloakUrl}/realms/{_realm}/protocol/openid-connect/token";

            // Note: clercqit-web is a public client, so we don't pass client_secret
            // Scopes are inherited from the client's default scopes (roles)
            var formData = new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = _clientId,
                ["username"] = username,
                ["password"] = password
            };

            if (!string.IsNullOrEmpty(totpCode))
            {
                formData["totp"] = totpCode;
            }

            var content = new FormUrlEncodedContent(formData);
            var response = await client.PostAsync(tokenEndpoint, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var tokenResponse = JsonSerializer.Deserialize<KeycloakTokenResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
                {
                    return new CloudIAMAuthResult
                    {
                        Success = false,
                        ErrorMessage = "Invalid token response from Keycloak"
                    };
                }

                // Parse the access token to extract user info
                var handler = new JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(tokenResponse.AccessToken);

                var userId = jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? "";
                var userName = jwt.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value ?? username;
                var email = jwt.Claims.FirstOrDefault(c => c.Type == "email")?.Value ?? "";
                var emailVerified = jwt.Claims.FirstOrDefault(c => c.Type == "email_verified")?.Value == "true";

                // Extract roles from realm_access.roles claim
                var roles = ExtractRolesFromToken(jwt);

                return new CloudIAMAuthResult
                {
                    Success = true,
                    AccessToken = tokenResponse.AccessToken,
                    User = new CloudIAMUser
                    {
                        Id = userId,
                        Username = userName,
                        Email = email,
                        Roles = roles,
                        EmailVerified = emailVerified,
                        MfaEnabled = false // Keycloak MFA is handled differently
                    }
                };
            }

            // Handle error responses
            var errorResponse = JsonSerializer.Deserialize<KeycloakErrorResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Check for MFA requirement
            if (errorResponse?.Error == "invalid_grant" &&
                errorResponse.ErrorDescription?.Contains("OTP", StringComparison.OrdinalIgnoreCase) == true)
            {
                return new CloudIAMAuthResult
                {
                    Success = false,
                    RequiresMfa = true,
                    ErrorMessage = "MFA code required"
                };
            }

            _logger.LogWarning("Keycloak authentication failed for user {Username}. Error: {Error}",
                username, errorResponse?.ErrorDescription ?? errorResponse?.Error);

            return new CloudIAMAuthResult
            {
                Success = false,
                ErrorMessage = errorResponse?.ErrorDescription ?? errorResponse?.Error ?? "Authentication failed"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error authenticating with local Keycloak for user {Username}", username);
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
            // First, get an admin access token
            var adminToken = await GetAdminTokenAsync();
            if (string.IsNullOrEmpty(adminToken))
            {
                return new CloudIAMAuthResult
                {
                    Success = false,
                    ErrorMessage = "Failed to obtain admin token for user registration"
                };
            }

            var client = _httpClientFactory.CreateClient();
            var usersEndpoint = $"{_keycloakUrl}/admin/realms/{_realm}/users";

            var userPayload = new
            {
                username,
                email,
                enabled = true,
                emailVerified = false,
                credentials = new[]
                {
                    new
                    {
                        type = "password",
                        value = password,
                        temporary = false
                    }
                }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, usersEndpoint);
            request.Headers.Add("Authorization", $"Bearer {adminToken}");
            request.Content = new StringContent(
                JsonSerializer.Serialize(userPayload),
                Encoding.UTF8,
                "application/json"
            );

            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Created)
            {
                // Get the user ID from the Location header
                var locationHeader = response.Headers.Location?.ToString();
                var userId = locationHeader?.Split('/').LastOrDefault() ?? Guid.NewGuid().ToString();

                return new CloudIAMAuthResult
                {
                    Success = true,
                    EmailVerificationRequired = "User registered successfully. For local development, email verification is disabled.",
                    User = new CloudIAMUser
                    {
                        Id = userId,
                        Username = username,
                        Email = email,
                        EmailVerified = false
                    }
                };
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var errorResponse = JsonSerializer.Deserialize<KeycloakErrorResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            _logger.LogWarning("Keycloak registration failed for user {Username}. Status: {StatusCode}, Error: {Error}",
                username, response.StatusCode, errorResponse?.ErrorMessage ?? responseContent);

            return new CloudIAMAuthResult
            {
                Success = false,
                ErrorMessage = errorResponse?.ErrorMessage ?? "Registration failed"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering user {Username} with local Keycloak", username);
            return new CloudIAMAuthResult
            {
                Success = false,
                ErrorMessage = "Registration service error"
            };
        }
    }

    public async Task<CloudIAMAuthResult> ValidateOAuthCallbackAsync(string provider, string code, string state)
    {
        // For local development, OAuth providers (GitHub, LinkedIn) are not configured
        // This would require setting up identity providers in Keycloak
        _logger.LogWarning("OAuth callback for provider {Provider} not supported in local development", provider);

        return await Task.FromResult(new CloudIAMAuthResult
        {
            Success = false,
            ErrorMessage = $"OAuth provider '{provider}' is not configured for local development. " +
                          "Please use username/password authentication or configure the identity provider in Keycloak."
        });
    }

    public async Task<CloudIAMUser?> GetUserByIdAsync(string userId)
    {
        try
        {
            var adminToken = await GetAdminTokenAsync();
            if (string.IsNullOrEmpty(adminToken))
            {
                return null;
            }

            var client = _httpClientFactory.CreateClient();
            var userEndpoint = $"{_keycloakUrl}/admin/realms/{_realm}/users/{userId}";

            var request = new HttpRequestMessage(HttpMethod.Get, userEndpoint);
            request.Headers.Add("Authorization", $"Bearer {adminToken}");

            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var keycloakUser = JsonSerializer.Deserialize<KeycloakUser>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (keycloakUser == null) return null;

                // Get user roles
                var roles = await GetUserRolesAsync(userId, adminToken);

                return new CloudIAMUser
                {
                    Id = keycloakUser.Id ?? userId,
                    Username = keycloakUser.Username ?? "",
                    Email = keycloakUser.Email ?? "",
                    Roles = roles,
                    EmailVerified = keycloakUser.EmailVerified,
                    MfaEnabled = false
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user {UserId} from local Keycloak", userId);
            return null;
        }
    }

    private async Task<string?> GetAdminTokenAsync()
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var tokenEndpoint = $"{_keycloakUrl}/realms/{_realm}/protocol/openid-connect/token";

            var formData = new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _adminClientId,
                ["client_secret"] = _adminClientSecret
            };

            var content = new FormUrlEncodedContent(formData);
            var response = await client.PostAsync(tokenEndpoint, content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonSerializer.Deserialize<KeycloakTokenResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return tokenResponse?.AccessToken;
            }

            _logger.LogWarning("Failed to get admin token from Keycloak. Status: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting admin token from local Keycloak");
            return null;
        }
    }

    private async Task<List<string>> GetUserRolesAsync(string userId, string adminToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var rolesEndpoint = $"{_keycloakUrl}/admin/realms/{_realm}/users/{userId}/role-mappings/realm";

            var request = new HttpRequestMessage(HttpMethod.Get, rolesEndpoint);
            request.Headers.Add("Authorization", $"Bearer {adminToken}");

            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var roles = JsonSerializer.Deserialize<List<KeycloakRole>>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return roles?.Select(r => r.Name ?? "").Where(n => !string.IsNullOrEmpty(n)).ToList() ?? new List<string>();
            }

            return new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting roles for user {UserId}", userId);
            return new List<string>();
        }
    }

    private List<string> ExtractRolesFromToken(JwtSecurityToken jwt)
    {
        var roles = new List<string>();

        // Try to get roles from the 'roles' claim directly (from our custom mapper)
        var rolesClaims = jwt.Claims.Where(c => c.Type == "roles").Select(c => c.Value);
        roles.AddRange(rolesClaims);

        // Also try realm_access.roles (standard Keycloak format)
        var realmAccessClaim = jwt.Claims.FirstOrDefault(c => c.Type == "realm_access")?.Value;
        if (!string.IsNullOrEmpty(realmAccessClaim))
        {
            try
            {
                var realmAccess = JsonSerializer.Deserialize<RealmAccess>(realmAccessClaim);
                if (realmAccess?.Roles != null)
                {
                    roles.AddRange(realmAccess.Roles);
                }
            }
            catch
            {
                // Ignore parsing errors
            }
        }

        return roles.Distinct().ToList();
    }

    // Internal DTOs for Keycloak responses
    private class KeycloakTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }
    }

    private class KeycloakErrorResponse
    {
        public string? Error { get; set; }

        [JsonPropertyName("error_description")]
        public string? ErrorDescription { get; set; }

        [JsonPropertyName("errorMessage")]
        public string? ErrorMessage { get; set; }
    }

    private class KeycloakUser
    {
        public string? Id { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public bool EmailVerified { get; set; }
        public bool Enabled { get; set; }
    }

    private class KeycloakRole
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
    }

    private class RealmAccess
    {
        public List<string>? Roles { get; set; }
    }
}
