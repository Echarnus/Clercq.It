namespace Clercq.It.Infrastructure.MyZen;

/// <summary>
/// The <c>MyZen</c> configuration section. Email and password come from the
/// Scaleway secret <c>myzen-login</c>, the session key from <c>myzen-session-key</c>
/// (infra/terraform/secrets.tf). See docs/myzen.md.
/// </summary>
public sealed class MyZenOptions
{
    public const string SectionName = "MyZen";

    public string BaseUrl { get; set; } = "https://myzen.trustteam.be";
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    /// <summary>Any long random string; the session cookies are encrypted with a key derived from it.</summary>
    public string SessionKey { get; set; } = string.Empty;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(Password) && !string.IsNullOrWhiteSpace(SessionKey);
}
