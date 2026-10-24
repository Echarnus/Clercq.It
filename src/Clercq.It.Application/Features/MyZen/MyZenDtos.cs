namespace Clercq.It.Application.Features.MyZen;

/// <summary>Session states reported by <c>GET /api/myzen/session</c>.</summary>
public static class MyZenSessionState
{
    public const string Disconnected = "Disconnected";
    public const string Connecting = "Connecting";
    public const string Connected = "Connected";
    public const string Expired = "Expired";
}

/// <summary>What the connect page shows next.</summary>
public static class MyZenLoginStep
{
    /// <summary>Ready to sign in; <see cref="MyZenLoginStepDto.CaptchaImage"/> is set when a CAPTCHA must be typed.</summary>
    public const string Credentials = "Credentials";
    /// <summary>MyZen asks for a code from the authenticator app.</summary>
    public const string Mfa = "Mfa";
    public const string Connected = "Connected";
}

public record MyZenStatusDto(string State, DateTimeOffset? ConnectedAt, DateTimeOffset? LastSeenAlive);

/// <summary>
/// The next login step. <c>CaptchaImage</c> is a <c>data:image/png;base64,...</c> URL, or null
/// when no CAPTCHA is asked; <c>Message</c> says why the previous step did not succeed.
/// </summary>
public record MyZenLoginStepDto(string Step, string? CaptchaImage, string? Message);

public record MyZenProjectDto(long Id, string Name, string Customer, string? StartDate, string? EndDate, string Status);

/// <summary>A MyZen time entry. <c>ProjectId</c> and <c>CustomerId</c> are only known when the entry is read on its own.</summary>
public record MyZenTimeEntryDto(
    long Id,
    DateOnly Date,
    string Description,
    long? ProjectId,
    string Project,
    long? CustomerId,
    string Customer,
    TimeSpan Duration,
    bool Invoiced);

/// <summary>A project MyZen suggests for a search, with the customer a time entry on it must carry.</summary>
public record MyZenProjectOptionDto(long Id, string Name, long? CustomerId, string Customer);

/// <summary>
/// A time entry to create or update. <c>CustomerId</c> is null for projects without a customer,
/// such as MyZen's absence projects (Feestdag, Vakantie, Ziekte).
/// </summary>
public record MyZenTimeEntryInput(
    DateOnly Date,
    int DurationMinutes,
    long ProjectId,
    long? CustomerId,
    string? Description,
    bool Invoiced);

/// <summary>Thrown when MyZen is asked for data without a live session; the API answers 409.</summary>
public sealed class MyZenNotConnectedException()
    : Exception("MyZen is not connected. Connect it again from the admin page.");

/// <summary>Thrown when MyZen answers in a way the client does not understand; the API answers 502.</summary>
public sealed class MyZenUnavailableException(string message) : Exception(message);

/// <summary>Thrown when MyZen refuses a change, with MyZen's reason when it gave one; the API answers 422.</summary>
public sealed class MyZenRejectedException(string message) : Exception(message);
