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

/// <summary>
/// A row of MyZen's project list. <c>CustomerId</c> comes from MyZen's customer list (matched on the
/// customer's name); it is null for projects without a customer, such as Feestdag, Vakantie and Ziekte.
/// </summary>
public record MyZenProjectDto(long Id, string Name, long? CustomerId, string Customer, string? StartDate, string? EndDate, string Status);

/// <summary>
/// One MyZen project as its edit page shows it. <c>Status</c> is MyZen's label (Prospect, Bezig, Klaar,
/// Geannuleerd); <c>TimeBudgetHours</c> is the time budget in hours.
/// </summary>
public record MyZenProjectDetailsDto(
    long Id,
    string Name,
    long? CustomerId,
    string? Customer,
    string Status,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string? Reference,
    string? Notes,
    decimal? Budget,
    decimal? TimeBudgetHours,
    decimal? Rate);

/// <summary>A row of MyZen's customer list (<c>/Company/Customer</c>).</summary>
public record MyZenCustomerSummaryDto(
    long Id,
    string? Number,
    string Name,
    string? Vat,
    string? Contact,
    string? City,
    string? Email,
    string? Phone);

/// <summary>A postal address as MyZen stores it; <c>Country</c> is an ISO code such as BE.</summary>
public record MyZenAddressDto(string? Street, string? Number, string? Box, string? Zipcode, string? City, string? Country);

/// <summary>
/// One MyZen customer as its edit page shows it. <c>Type</c> is Company or Private; <c>Vat</c> is
/// written like BE0825170684; <c>Language</c> is MyZen's code (NL, FR, EN, ...).
/// </summary>
public record MyZenCustomerDto(
    long Id,
    string? Number,
    string Name,
    string? CommercialName,
    string? Type,
    string? Vat,
    bool VatLiable,
    string? Email,
    string? Phone,
    string? Mobile,
    string? Website,
    string? Language,
    string? Reference,
    string? ContactFirstName,
    string? ContactLastName,
    MyZenAddressDto Address);

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

/// <summary>
/// A project MyZen suggests for a search, with the customer a time entry on it must carry.
/// <c>CustomerId</c> and <c>Customer</c> are null for projects without a customer (Feestdag, Vakantie, Ziekte).
/// </summary>
public record MyZenProjectOptionDto(long Id, string Name, long? CustomerId, string? Customer);

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
