namespace Clercq.It.Api.Features.Auth;

/// <summary>
/// Authenticates against Keycloak (Cloud IAM in production, the Aspire container locally).
/// </summary>
public interface ICloudIAMAuthService
{
    Task<CloudIAMAuthResult> AuthenticateAsync(string username, string password, string? totpCode = null);
    Task<CloudIAMUser?> GetUserByIdAsync(string userId);
}

public class CloudIAMAuthResult
{
    public bool Success { get; set; }
    public bool RequiresMfa { get; set; }
    public string? ErrorMessage { get; set; }
    public CloudIAMUser? User { get; set; }
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
