using System.Net;
using System.Text.Json;
using Clercq.It.Api.Features.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;
using FluentAssertions;

namespace ClercqIt.Api.Tests.Auth;

public class CloudIAMAuthServiceTests
{
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<CloudIAMAuthService>> _mockLogger;

    public CloudIAMAuthServiceTests()
    {
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<CloudIAMAuthService>>();
    }

    private CloudIAMAuthService CreateService()
    {
        _mockConfiguration.Setup(x => x["CloudIAM:ApiUrl"])
            .Returns("https://cloudiam.example.com");
        _mockConfiguration.Setup(x => x["CloudIAM:ApiKey"])
            .Returns("test-api-key");

        return new CloudIAMAuthService(
            _mockHttpClientFactory.Object,
            _mockConfiguration.Object,
            _mockLogger.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WhenApiUrlMissing_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockConfiguration.Setup(x => x["CloudIAM:ApiUrl"])
            .Returns((string?)null);
        _mockConfiguration.Setup(x => x["CloudIAM:ApiKey"])
            .Returns("test-api-key");

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new CloudIAMAuthService(
                _mockHttpClientFactory.Object,
                _mockConfiguration.Object,
                _mockLogger.Object));

        exception.Message.Should().Contain("CloudIAM:ApiUrl");
    }

    [Fact]
    public void Constructor_WhenApiKeyMissing_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockConfiguration.Setup(x => x["CloudIAM:ApiUrl"])
            .Returns("https://cloudiam.example.com");
        _mockConfiguration.Setup(x => x["CloudIAM:ApiKey"])
            .Returns((string?)null);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new CloudIAMAuthService(
                _mockHttpClientFactory.Object,
                _mockConfiguration.Object,
                _mockLogger.Object));

        exception.Message.Should().Contain("CloudIAM:ApiKey");
    }

    #endregion

    #region AuthenticateAsync Tests

    [Fact]
    public async Task AuthenticateAsync_WithValidCredentials_ReturnsSuccessResult()
    {
        // Arrange
        var authService = CreateService();
        var loginResponse = new
        {
            user = new
            {
                id = "user-123",
                username = "testuser",
                email = "test@example.com",
                roles = new[] { "Admin", "User" },
                emailVerified = true,
                mfaEnabled = false
            },
            accessToken = "valid-access-token"
        };

        var mockHandler = CreateMockHttpHandler(
            HttpStatusCode.OK,
            JsonSerializer.Serialize(loginResponse));

        var httpClient = new HttpClient(mockHandler.Object);
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        // Act
        var result = await authService.AuthenticateAsync("testuser", "password123");

        // Assert
        result.Success.Should().BeTrue();
        result.AccessToken.Should().Be("valid-access-token");
        result.User.Should().NotBeNull();
        result.User!.Id.Should().Be("user-123");
        result.User.Username.Should().Be("testuser");
        result.User.Email.Should().Be("test@example.com");
        result.User.Roles.Should().Contain("Admin");
        result.User.EmailVerified.Should().BeTrue();
        result.User.MfaEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task AuthenticateAsync_WithTokenAlternativeField_ReturnsSuccessResult()
    {
        // Arrange
        var authService = CreateService();
        var loginResponse = new
        {
            user = new
            {
                id = "user-123",
                username = "testuser",
                email = "test@example.com",
                roles = new[] { "User" },
                emailVerified = true,
                mfaEnabled = false
            },
            token = "alternative-token-field" // Some APIs use 'token' instead of 'accessToken'
        };

        var mockHandler = CreateMockHttpHandler(
            HttpStatusCode.OK,
            JsonSerializer.Serialize(loginResponse));

        var httpClient = new HttpClient(mockHandler.Object);
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        // Act
        var result = await authService.AuthenticateAsync("testuser", "password123");

        // Assert
        result.Success.Should().BeTrue();
        result.AccessToken.Should().Be("alternative-token-field");
    }

    [Fact]
    public async Task AuthenticateAsync_WithInvalidCredentials_ReturnsFailureResult()
    {
        // Arrange
        var authService = CreateService();
        var errorResponse = new
        {
            code = "INVALID_CREDENTIALS",
            message = "Invalid username or password"
        };

        var mockHandler = CreateMockHttpHandler(
            HttpStatusCode.Unauthorized,
            JsonSerializer.Serialize(errorResponse));

        var httpClient = new HttpClient(mockHandler.Object);
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        // Act
        var result = await authService.AuthenticateAsync("wronguser", "wrongpassword");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Invalid username or password");
        result.User.Should().BeNull();
    }

    [Fact]
    public async Task AuthenticateAsync_WhenMfaRequired_ReturnsRequiresMfaResult()
    {
        // Arrange
        var authService = CreateService();
        var errorResponse = new
        {
            code = "MFA_REQUIRED",
            message = "Multi-factor authentication required"
        };

        var mockHandler = CreateMockHttpHandler(
            HttpStatusCode.PreconditionRequired,
            JsonSerializer.Serialize(errorResponse));

        var httpClient = new HttpClient(mockHandler.Object);
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        // Act
        var result = await authService.AuthenticateAsync("testuser", "password123");

        // Assert
        result.Success.Should().BeFalse();
        result.RequiresMfa.Should().BeTrue();
        result.ErrorMessage.Should().Be("MFA code required");
    }

    [Fact]
    public async Task AuthenticateAsync_WhenMfaRequiredViaMessage_ReturnsRequiresMfaResult()
    {
        // Arrange
        var authService = CreateService();
        var errorResponse = new
        {
            code = "AUTH_ERROR",
            message = "MFA verification required for this account"
        };

        var mockHandler = CreateMockHttpHandler(
            HttpStatusCode.Unauthorized,
            JsonSerializer.Serialize(errorResponse));

        var httpClient = new HttpClient(mockHandler.Object);
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        // Act
        var result = await authService.AuthenticateAsync("testuser", "password123");

        // Assert
        result.Success.Should().BeFalse();
        result.RequiresMfa.Should().BeTrue();
    }

    [Fact]
    public async Task AuthenticateAsync_WithTotpCode_IncludesTotpInPayload()
    {
        // Arrange
        var authService = CreateService();
        var loginResponse = new
        {
            user = new
            {
                id = "user-123",
                username = "testuser",
                email = "test@example.com",
                roles = new[] { "User" },
                emailVerified = true,
                mfaEnabled = true
            },
            accessToken = "valid-token"
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
                Content = new StringContent(JsonSerializer.Serialize(loginResponse))
            });

        var httpClient = new HttpClient(mockHandler.Object);
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        // Act
        await authService.AuthenticateAsync("testuser", "password123", "123456");

        // Assert
        capturedRequest.Should().NotBeNull();
        var content = await capturedRequest!.Content!.ReadAsStringAsync();
        content.Should().Contain("totp_code");
        content.Should().Contain("123456");
    }

    [Fact]
    public async Task AuthenticateAsync_WhenInvalidResponse_ReturnsFailureResult()
    {
        // Arrange
        var authService = CreateService();
        var loginResponse = new
        {
            user = (object?)null,
            accessToken = "token"
        };

        var mockHandler = CreateMockHttpHandler(
            HttpStatusCode.OK,
            JsonSerializer.Serialize(loginResponse));

        var httpClient = new HttpClient(mockHandler.Object);
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        // Act
        var result = await authService.AuthenticateAsync("testuser", "password123");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid response");
    }

    [Fact]
    public async Task AuthenticateAsync_WhenExceptionThrown_ReturnsServiceError()
    {
        // Arrange
        var authService = CreateService();
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
        var result = await authService.AuthenticateAsync("testuser", "password123");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Authentication service error");
    }

    [Fact]
    public async Task AuthenticateAsync_SetsCorrectAuthHeaders()
    {
        // Arrange
        var authService = CreateService();
        var loginResponse = new
        {
            user = new
            {
                id = "user-123",
                username = "testuser",
                email = "test@example.com",
                roles = new string[0],
                emailVerified = true,
                mfaEnabled = false
            },
            accessToken = "token"
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
                Content = new StringContent(JsonSerializer.Serialize(loginResponse))
            });

        var httpClient = new HttpClient(mockHandler.Object);
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        // Act
        await authService.AuthenticateAsync("testuser", "password123");

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Headers.Authorization.Should().NotBeNull();
        capturedRequest.Headers.Authorization!.Scheme.Should().Be("Bearer");
        capturedRequest.Headers.Authorization.Parameter.Should().Be("test-api-key");
        capturedRequest.Headers.Contains("X-API-Key").Should().BeTrue();
    }

    #endregion

    #region RegisterUserAsync Tests

    [Fact]
    public async Task RegisterUserAsync_WithValidData_ReturnsSuccessResult()
    {
        // Arrange
        var authService = CreateService();
        var registerResponse = new
        {
            user = new
            {
                id = "new-user-123",
                username = "newuser",
                email = "new@example.com",
                emailVerified = false
            },
            emailSent = true
        };

        var mockHandler = CreateMockHttpHandler(
            HttpStatusCode.OK,
            JsonSerializer.Serialize(registerResponse));

        var httpClient = new HttpClient(mockHandler.Object);
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        // Act
        var result = await authService.RegisterUserAsync("newuser", "new@example.com", "Password123!");

        // Assert
        result.Success.Should().BeTrue();
        result.EmailVerificationRequired.Should().Contain("verification email");
        result.User.Should().NotBeNull();
        result.User!.Username.Should().Be("newuser");
        result.User.Email.Should().Be("new@example.com");
        result.User.EmailVerified.Should().BeFalse();
    }

    [Fact]
    public async Task RegisterUserAsync_WhenUserExists_ReturnsFailure()
    {
        // Arrange
        var authService = CreateService();
        var errorResponse = new
        {
            code = "USER_EXISTS",
            message = "A user with this email already exists"
        };

        var mockHandler = CreateMockHttpHandler(
            HttpStatusCode.Conflict,
            JsonSerializer.Serialize(errorResponse));

        var httpClient = new HttpClient(mockHandler.Object);
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        // Act
        var result = await authService.RegisterUserAsync("existinguser", "existing@example.com", "Password123!");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("already exists");
    }

    [Fact]
    public async Task RegisterUserAsync_WhenExceptionThrown_ReturnsServiceError()
    {
        // Arrange
        var authService = CreateService();
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
        var result = await authService.RegisterUserAsync("newuser", "new@example.com", "Password123!");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Registration service error");
    }

    #endregion

    #region ValidateOAuthCallbackAsync Tests

    [Fact]
    public async Task ValidateOAuthCallbackAsync_WithValidCallback_ReturnsSuccessResult()
    {
        // Arrange
        var authService = CreateService();
        var oauthResponse = new
        {
            user = new
            {
                id = "oauth-user-123",
                username = "oauthuser",
                email = "oauth@example.com",
                roles = new[] { "User" },
                emailVerified = true
            },
            accessToken = "oauth-access-token"
        };

        var mockHandler = CreateMockHttpHandler(
            HttpStatusCode.OK,
            JsonSerializer.Serialize(oauthResponse));

        var httpClient = new HttpClient(mockHandler.Object);
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        // Act
        var result = await authService.ValidateOAuthCallbackAsync("github", "auth-code", "state-123");

        // Assert
        result.Success.Should().BeTrue();
        result.AccessToken.Should().Be("oauth-access-token");
        result.User.Should().NotBeNull();
        result.User!.Id.Should().Be("oauth-user-123");
        result.User.Username.Should().Be("oauthuser");
        result.User.Email.Should().Be("oauth@example.com");
    }

    [Fact]
    public async Task ValidateOAuthCallbackAsync_WhenInvalidResponse_ReturnsFailure()
    {
        // Arrange
        var authService = CreateService();
        var oauthResponse = new
        {
            user = (object?)null,
            accessToken = "token"
        };

        var mockHandler = CreateMockHttpHandler(
            HttpStatusCode.OK,
            JsonSerializer.Serialize(oauthResponse));

        var httpClient = new HttpClient(mockHandler.Object);
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        // Act
        var result = await authService.ValidateOAuthCallbackAsync("github", "code", "state");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid OAuth response");
    }

    [Fact]
    public async Task ValidateOAuthCallbackAsync_WhenOAuthFails_ReturnsFailure()
    {
        // Arrange
        var authService = CreateService();
        var errorResponse = new
        {
            code = "OAUTH_ERROR",
            message = "Invalid authorization code"
        };

        var mockHandler = CreateMockHttpHandler(
            HttpStatusCode.BadRequest,
            JsonSerializer.Serialize(errorResponse));

        var httpClient = new HttpClient(mockHandler.Object);
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        // Act
        var result = await authService.ValidateOAuthCallbackAsync("github", "invalid-code", "state");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid authorization code");
    }

    [Fact]
    public async Task ValidateOAuthCallbackAsync_WhenExceptionThrown_ReturnsServiceError()
    {
        // Arrange
        var authService = CreateService();
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
        var result = await authService.ValidateOAuthCallbackAsync("github", "code", "state");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("OAuth service error");
    }

    #endregion

    #region GetUserByIdAsync Tests

    [Fact]
    public async Task GetUserByIdAsync_WhenUserExists_ReturnsUser()
    {
        // Arrange
        var authService = CreateService();
        var userResponse = new
        {
            user = new
            {
                id = "user-123",
                username = "testuser",
                email = "test@example.com",
                roles = new[] { "Admin", "User" },
                emailVerified = true,
                mfaEnabled = true
            }
        };

        var mockHandler = CreateMockHttpHandler(
            HttpStatusCode.OK,
            JsonSerializer.Serialize(userResponse));

        var httpClient = new HttpClient(mockHandler.Object);
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        // Act
        var result = await authService.GetUserByIdAsync("user-123");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("user-123");
        result.Username.Should().Be("testuser");
        result.Email.Should().Be("test@example.com");
        result.Roles.Should().Contain("Admin");
        result.Roles.Should().Contain("User");
        result.EmailVerified.Should().BeTrue();
        result.MfaEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task GetUserByIdAsync_WhenUserNotFound_ReturnsNull()
    {
        // Arrange
        var authService = CreateService();
        var mockHandler = CreateMockHttpHandler(
            HttpStatusCode.NotFound,
            JsonSerializer.Serialize(new { message = "User not found" }));

        var httpClient = new HttpClient(mockHandler.Object);
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        // Act
        var result = await authService.GetUserByIdAsync("nonexistent-user");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserByIdAsync_WhenNullUserInResponse_ReturnsNull()
    {
        // Arrange
        var authService = CreateService();
        var userResponse = new
        {
            user = (object?)null
        };

        var mockHandler = CreateMockHttpHandler(
            HttpStatusCode.OK,
            JsonSerializer.Serialize(userResponse));

        var httpClient = new HttpClient(mockHandler.Object);
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        // Act
        var result = await authService.GetUserByIdAsync("user-123");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserByIdAsync_WhenExceptionThrown_ReturnsNull()
    {
        // Arrange
        var authService = CreateService();
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
        var result = await authService.GetUserByIdAsync("user-123");

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

    #endregion
}
