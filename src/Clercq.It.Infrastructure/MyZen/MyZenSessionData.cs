namespace Clercq.It.Infrastructure.MyZen;

/// <summary>The secret part of a MyZen session: its cookies and the login page's tokens.</summary>
public sealed record MyZenSessionData(
    IReadOnlyList<MyZenCookie> Cookies,
    string? FormToken = null,
    string? HeaderToken = null,
    string? CaptchaCheck = null);

public sealed record MyZenCookie(string Name, string Value, string Domain, string Path, DateTime? Expires, bool Secure, bool HttpOnly);
