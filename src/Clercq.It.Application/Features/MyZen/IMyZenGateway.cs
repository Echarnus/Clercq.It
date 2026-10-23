namespace Clercq.It.Application.Features.MyZen;

/// <summary>
/// The owner's own MyZen (myzen.trustteam.be) account, reached with his stored
/// credentials and a session he unlocks himself (CAPTCHA and MFA). Issue #345;
/// see docs/myzen.md.
/// </summary>
public interface IMyZenGateway
{
    /// <summary>Whether a MyZen session is connected, being set up, or expired.</summary>
    Task<MyZenStatusDto> GetStatusAsync(CancellationToken cancellationToken);

    /// <summary>Opens the MyZen login page and returns what the owner must fill in.</summary>
    Task<MyZenLoginStepDto> StartLoginAsync(CancellationToken cancellationToken);

    /// <summary>Signs in with the stored credentials and the CAPTCHA the owner typed, if one was shown.</summary>
    Task<MyZenLoginStepDto> SubmitLoginAsync(string? captcha, CancellationToken cancellationToken);

    /// <summary>Completes the sign-in with the owner's MFA code.</summary>
    Task<MyZenLoginStepDto> SubmitMfaCodeAsync(string code, CancellationToken cancellationToken);

    /// <summary>Forgets the MyZen session.</summary>
    Task DisconnectAsync(CancellationToken cancellationToken);

    /// <summary>Touches the session so MyZen keeps it; false when it has expired.</summary>
    Task<bool> KeepAliveAsync(CancellationToken cancellationToken);

    /// <exception cref="MyZenNotConnectedException">No live session.</exception>
    Task<IReadOnlyList<MyZenProjectDto>> GetProjectsAsync(CancellationToken cancellationToken);

    /// <exception cref="MyZenNotConnectedException">No live session.</exception>
    Task<IReadOnlyList<MyZenTimeEntryDto>> GetTimeEntriesAsync(DateOnly firstDay, DateOnly lastDay, CancellationToken cancellationToken);

    /// <exception cref="MyZenNotConnectedException">No live session.</exception>
    Task<MyZenTimeEntryDto?> GetTimeEntryAsync(long id, CancellationToken cancellationToken);
}
