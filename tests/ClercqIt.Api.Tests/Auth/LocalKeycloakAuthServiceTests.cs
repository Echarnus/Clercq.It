using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Clercq.It.Api.Features.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Moq.Protected;
using Xunit;
using FluentAssertions;

namespace ClercqIt.Api.Tests.Auth;

public class LocalKeycloakAuthServiceTests
{
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly Mock<ILogger<LocalKeycloakAuthService>> _mockLogger;
    private readonly LocalKeycloakAuthService _authService;

    public LocalKeycloakAuthServiceTests()
    {
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockLogger = new Mock<ILogger<LocalKeycloakAuthService>>();

        // Use in-memory configuration instead of mocking
        _configuration = CreateTestConfiguration();

        _authService = new LocalKeycloakAuthService(
            _mockHttpClientFactory.Object,
            _configuration,
            _mockLogger.Object);
    }

    private static IConfiguration CreateTestConfiguration()
    {
        var configData = new Dictionary<string, string?>
        {
            ["ConnectionStrings:keycloak"] = "http://localhost:8080",
            ["Keycloak:Realm"] = "clercqit",
            ["Keycloak:ClientId"] = "clercqit-web",
            ["Keycloak:ClientSecret"] = "test-secret",
            ["Keycloak:AdminClientId"] = "clercqit-admin",
            ["Keycloak:AdminClientSecret"] = "admin-secret"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
    }

    #region AuthenticateAsync Tests

    [Fact]
    public async Task AuthenticateAsync_WithValidCredentials_ReturnsSuccessResult()
    {
        // Arrange
        var accessToken = CreateMockJwtToken("test-user-id", "testuser", "test@example.com", true, new[] { "Admin" });
        var tokenResponse = new
        {
            access_token = accessToken,
            refresh_token = "mock-refresh-token",
            expires_in = 3600,
            token_type = "Bearer"
        };

        var mockHandler = CreateMockHttpHandler(
            HttpStatusCode.OK,
            JsonSerializer.Serialize(tokenResponse));

        var httpClient = new HttpClient(mockHandler.Object);
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        // Act
        var result = await _authService.AuthenticateAsync("testuser", "password123");

        // Assert
        result.Success.Should().BeTrue();
        result.AccessToken.Should().Be(accessToken);
        result.User.Should().NotBeNull();
        result.User!.Username.Should().Be("testuser");
        result.User.Email.Should().Be("test@example.com");
        result.User.Id.Should().Be("test-user-id");
        result.RequiresMfa.Should().BeFalse();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task AuthenticateAsync_WithInvalidCredentials_ReturnsFailureResult()
    {
        // Arrange
        var errorResponse = new
        {
            error = "invalid_grant",
            error_description = "Invalid user credentials"
        };

        var mockHandler = CreateMockHttpHandler(
            HttpStatusCode.Unauthorized,
            JsonSerializer.Serialize(errorResponse));

        var httpClient = new HttpClient(mockHandler.Object);
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        // Act
        var result = await _authService.AuthenticateAsync("wronguser", "wrongpassword");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Invalid user credentials");
        result.User.Should().BeNull();
        result.AccessToken.Should().BeNull();
    }

    [Fact]
    public async Task AuthenticateAsync_WhenMfaRequired_ReturnsRequiresMfaResult()
    {
        // Arrange
        var errorResponse = new
        {
            error = "invalid_grant",
            error_description = "OTP code required"
        };

        var mockHandler = CreateMockHttpHandler(
            HttpStatusCode.Unauthorized,
            JsonSerializer.Serialize(errorResponse));

        var httpClient = new HttpClient(mockHandler.Object);
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        // Act
        var result = await _authService.AuthenticateAsync("testuser", "password123");

        // Assert
        result.Success.Should().BeFalse();
        result.RequiresMfa.Should().BeTrue();
        result.ErrorMessage.Should().Be("MFA code required");
    }

    [Fact]
    public async Task AuthenticateAsync_WithTotpCode_IncludesTotpInRequest()
    {
        // Arrange
        var accessToken = CreateMockJwtToken("test-user-id", "testuser", "test@example.com", true, new[] { "User" });
        var tokenResponse = new
        {
            access_token = accessToken,
            refresh_token = "mock-refresh-token",
            expires_in = 3600,
            token_type = "Bearer"
        };

        HttpRequestMessage? capturedRequest = null;
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(tokenResponse))
            });

        var httpClient = new HttpClient(mockHandler.Object);
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        // Act
        await _authService.AuthenticateAsync("testuser", "password123", "123456");

        // Assert
        capturedRequest.Should().NotBeNull();
        var content = await capturedRequest!.Content!.ReadAsStringAsync();
        content.Should().Contain("totp=123456");
    }

    [Fact]
    public async Task AuthenticateAsync_WhenInvalidTokenResponse_ReturnsFailureResult()
    {
        // Arrange
        var tokenResponse = new
        {
            access_token = (string?)null,
            refresh_token = "mock-refresh-token",
            expires_in = 3600
        };

        var mockHandler = CreateMockHttpHandler(
            HttpStatusCode.OK,
            JsonSerializer.Serialize(tokenResponse));

        var httpClient = new HttpClient(mockHandler.Object);
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        // Act
        var result = await _authService.AuthenticateAsync("testuser", "password123");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Invalid token response from Keycloak");
    }

    [Fact]
    public async Task AuthenticateAsync_WhenHttpClientThrowsException_ReturnsServiceError()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var httpClient = new HttpClient(mockHandler.Object);
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        // Act
        var result = await _authService.AuthenticateAsync("testuser", "password123");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Authentication service error");
    }

    [Fact]
    public async Task AuthenticateAsync_ExtractsRolesFromToken()
    {
        // Arrange
        var roles = new[] { "Admin", "Blogs.Contributor", "Projects.Contributor" };
        var accessToken = CreateMockJwtToken("test-user-id", "testuser", "test@example.com", true, roles);
        var tokenResponse = new
        {
            access_token = accessToken,
            refresh_token = "mock-refresh-token",
            expires_in = 3600,
            token_type = "Bearer"
        };

        var mockHandler = CreateMockHttpHandler(
            HttpStatusCode.OK,
            JsonSerializer.Serialize(tokenResponse));

        var httpClient = new HttpClient(mockHandler.Object);
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        // Act
        var result = await _authService.AuthenticateAsync("testuser", "password123");

        // Assert
        result.Success.Should().BeTrue();
        result.User.Should().NotBeNull();
        result.User!.Roles.Should().Contain("Admin");
        result.User.Roles.Should().Contain("Blogs.Contributor");
        result.User.Roles.Should().Contain("Projects.Contributor");
    }

    #endregion

    #region RegisterUserAsync Tests

    [Fact]
    public async Task RegisterUserAsync_WithValidData_ReturnsSuccessResult()
    {
        // Arrange
        var adminTokenResponse = new
        {
            access_token = "admin-token",
            token_type = "Bearer"
        };

        var callCount = 0;
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount == 1) // Admin token request
                {
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(JsonSerializer.Serialize(adminTokenResponse))
                    };
                }
                else // User creation request
                {
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.Created,
                        Headers = { Location = new Uri("http://localhost:8080/admin/realms/clercqit/users/new-user-id") }
                    };
                }
            });

        var httpClient = new HttpClient(mockHandler.Object);
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        // Act
        var result = await _authService.RegisterUserAsync("newuser", "newuser@example.com", "Password123!");

        // Assert
        result.Success.Should().BeTrue();
        result.User.Should().NotBeNull();
        result.User!.Username.Should().Be("newuser");
        result.User.Email.Should().Be("newuser@example.com");
        result.EmailVerificationRequired.Should().Contain("User registered successfully");
    }

    [Fact]
    public async Task RegisterUserAsync_WhenAdminTokenFails_ReturnsFailure()
    {
        // Arrange
        var mockHandler = CreateMockHttpHandler(
            HttpStatusCode.Unauthorized,
            JsonSerializer.Serialize(new { error = "invalid_client" }));

        var httpClient = new HttpClient(mockHandler.Object);
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        // Act
        var result = await _authService.RegisterUserAsync("newuser", "newuser@example.com", "Password123!");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("admin token");
    }

    [Fact]
    public async Task RegisterUserAsync_WhenUserCreationFails_ReturnsFailure()
    {
        // Arrange
        var adminTokenResponse = new
        {
            access_token = "admin-token",
            token_type = "Bearer"
        };

        var errorResponse = new
        {
            errorMessage = "User exists with same username"
        };

        var callCount = 0;
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount == 1)
                {
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(JsonSerializer.Serialize(adminTokenResponse))
                    };
                }
                else
                {
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.Conflict,
                        Content = new StringContent(JsonSerializer.Serialize(errorResponse))
                    };
                }
            });

        var httpClient = new HttpClient(mockHandler.Object);
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        // Act
        var result = await _authService.RegisterUserAsync("existinguser", "existing@example.com", "Password123!");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("User exists with same username");
    }

    [Fact]
    public async Task RegisterUserAsync_WhenNetworkExceptionDuringAdminToken_ReturnsAdminTokenError()
    {
        // Arrange - Exception is thrown during GetAdminToken, which is caught
        // and returns null, then RegisterUserAsync returns "Failed to obtain admin token"
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var httpClient = new HttpClient(mockHandler.Object);
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        // Act
        var result = await _authService.RegisterUserAsync("newuser", "newuser@example.com", "Password123!");

        // Assert
        result.Success.Should().BeFalse();
        // The exception in GetAdminToken is caught and returns null,
        // then RegisterUserAsync checks for null admin token
        result.ErrorMessage.Should().Contain("admin token");
    }

    [Fact]
    public async Task RegisterUserAsync_WhenExceptionDuringUserCreation_ReturnsServiceError()
    {
        // Arrange - First call succeeds (admin token), second call throws exception
        var adminTokenResponse = new
        {
            access_token = "admin-token",
            token_type = "Bearer"
        };

        var callCount = 0;
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount == 1)
                {
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(JsonSerializer.Serialize(adminTokenResponse))
                    };
                }
                throw new HttpRequestException("Network error during user creation");
            });

        var httpClient = new HttpClient(mockHandler.Object);
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        // Act
        var result = await _authService.RegisterUserAsync("newuser", "newuser@example.com", "Password123!");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Registration service error");
    }

    #endregion

    #region ValidateOAuthCallbackAsync Tests

    [Fact]
    public async Task ValidateOAuthCallbackAsync_ForGitHub_ReturnsNotSupportedError()
    {
        // Act
        var result = await _authService.ValidateOAuthCallbackAsync("github", "code123", "state456");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("github");
        result.ErrorMessage.Should().Contain("not configured for local development");
    }

    [Fact]
    public async Task ValidateOAuthCallbackAsync_ForLinkedIn_ReturnsNotSupportedError()
    {
        // Act
        var result = await _authService.ValidateOAuthCallbackAsync("linkedin", "code123", "state456");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("linkedin");
        result.ErrorMessage.Should().Contain("not configured for local development");
    }

    #endregion

    #region GetUserByIdAsync Tests

    [Fact]
    public async Task GetUserByIdAsync_WhenUserExists_ReturnsUser()
    {
        // Arrange
        var adminTokenResponse = new { access_token = "admin-token", token_type = "Bearer" };
        var userResponse = new
        {
            id = "user-123",
            username = "testuser",
            email = "test@example.com",
            emailVerified = true,
            enabled = true
        };
        var rolesResponse = new[]
        {
            new { id = "role-1", name = "Admin" },
            new { id = "role-2", name = "User" }
        };

        var callCount = 0;
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage request, CancellationToken _) =>
            {
                callCount++;
                if (callCount == 1) // Admin token
                {
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(JsonSerializer.Serialize(adminTokenResponse))
                    };
                }
                else if (callCount == 2) // User info
                {
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(JsonSerializer.Serialize(userResponse))
                    };
                }
                else // Roles
                {
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(JsonSerializer.Serialize(rolesResponse))
                    };
                }
            });

        var httpClient = new HttpClient(mockHandler.Object);
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        // Act
        var result = await _authService.GetUserByIdAsync("user-123");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("user-123");
        result.Username.Should().Be("testuser");
        result.Email.Should().Be("test@example.com");
        result.EmailVerified.Should().BeTrue();
        result.Roles.Should().Contain("Admin");
        result.Roles.Should().Contain("User");
    }

    [Fact]
    public async Task GetUserByIdAsync_WhenUserNotFound_ReturnsNull()
    {
        // Arrange
        var adminTokenResponse = new { access_token = "admin-token", token_type = "Bearer" };

        var callCount = 0;
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount == 1)
                {
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(JsonSerializer.Serialize(adminTokenResponse))
                    };
                }
                else
                {
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.NotFound
                    };
                }
            });

        var httpClient = new HttpClient(mockHandler.Object);
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        // Act
        var result = await _authService.GetUserByIdAsync("nonexistent-user");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserByIdAsync_WhenAdminTokenFails_ReturnsNull()
    {
        // Arrange
        var mockHandler = CreateMockHttpHandler(
            HttpStatusCode.Unauthorized,
            JsonSerializer.Serialize(new { error = "invalid_client" }));

        var httpClient = new HttpClient(mockHandler.Object);
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        // Act
        var result = await _authService.GetUserByIdAsync("user-123");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserByIdAsync_WhenExceptionThrown_ReturnsNull()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var httpClient = new HttpClient(mockHandler.Object);
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        // Act
        var result = await _authService.GetUserByIdAsync("user-123");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Helper Methods

    private static Mock<HttpMessageHandler> CreateMockHttpHandler(HttpStatusCode statusCode, string content)
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content)
            });
        return mockHandler;
    }

    private static string CreateMockJwtToken(
        string userId,
        string username,
        string email,
        bool emailVerified,
        string[] roles)
    {
        var claims = new List<Claim>
        {
            new("sub", userId),
            new("preferred_username", username),
            new("email", email),
            new("email_verified", emailVerified.ToString().ToLower())
        };

        // Add roles claims
        foreach (var role in roles)
        {
            claims.Add(new Claim("roles", role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("test-secret-key-that-is-long-enough-for-hs256-algorithm"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "http://localhost:8080/realms/clercqit",
            audience: "clercqit-web",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    #endregion
}
