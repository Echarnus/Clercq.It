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

    /// <summary>Every project in MyZen, with its customer's ID when MyZen's customer list has that customer once.</summary>
    /// <exception cref="MyZenNotConnectedException">No live session.</exception>
    Task<IReadOnlyList<MyZenProjectDto>> GetProjectsAsync(CancellationToken cancellationToken);

    /// <summary>One project from its edit page, or null when MyZen has none with this ID.</summary>
    /// <exception cref="MyZenNotConnectedException">No live session.</exception>
    Task<MyZenProjectDetailsDto?> GetProjectAsync(long id, CancellationToken cancellationToken);

    /// <summary>
    /// MyZen's customers; with a <paramref name="keyword"/>, only those MyZen's own list search
    /// matches on it (name, VAT or company number).
    /// </summary>
    /// <exception cref="MyZenNotConnectedException">No live session.</exception>
    Task<IReadOnlyList<MyZenCustomerSummaryDto>> GetCustomersAsync(string? keyword, CancellationToken cancellationToken);

    /// <summary>One customer from its edit page, or null when MyZen has none with this ID.</summary>
    /// <exception cref="MyZenNotConnectedException">No live session.</exception>
    Task<MyZenCustomerDto?> GetCustomerAsync(long id, CancellationToken cancellationToken);

    /// <summary>Creates a customer from MyZen's new-customer form; the new ID when MyZen's answer shows it.</summary>
    /// <exception cref="MyZenNotConnectedException">No live session.</exception>
    /// <exception cref="MyZenRejectedException">MyZen refused the customer.</exception>
    Task<long?> CreateCustomerAsync(MyZenCustomerInput customer, CancellationToken cancellationToken);

    /// <summary>Changes the given fields of a customer, keeping the rest of its form; false when MyZen has none with this ID.</summary>
    /// <exception cref="MyZenNotConnectedException">No live session.</exception>
    /// <exception cref="MyZenRejectedException">MyZen refused the change.</exception>
    Task<bool> UpdateCustomerAsync(long id, MyZenCustomerInput customer, CancellationToken cancellationToken);

    /// <summary>Creates a project for the customer named <paramref name="customerName"/>; the new ID when MyZen's answer shows it.</summary>
    /// <exception cref="MyZenNotConnectedException">No live session.</exception>
    /// <exception cref="MyZenRejectedException">MyZen refused the project.</exception>
    Task<long?> CreateProjectAsync(MyZenProjectInput project, string customerName, CancellationToken cancellationToken);

    /// <summary>
    /// Changes the given fields of a project, keeping the rest of its form; false when MyZen has none
    /// with this ID. <paramref name="customerName"/> is set when <c>CustomerId</c> changes.
    /// </summary>
    /// <exception cref="MyZenNotConnectedException">No live session.</exception>
    /// <exception cref="MyZenRejectedException">MyZen refused the change.</exception>
    Task<bool> UpdateProjectAsync(long id, MyZenProjectInput project, string? customerName, CancellationToken cancellationToken);

    /// <summary>The time entries from <paramref name="firstDay"/> to <paramref name="lastDay"/>.</summary>
    /// <exception cref="MyZenNotConnectedException">No live session.</exception>
    Task<IReadOnlyList<MyZenTimeEntryDto>> GetTimeEntriesAsync(DateOnly firstDay, DateOnly lastDay, CancellationToken cancellationToken);

    /// <summary>One time entry with its project and customer IDs, or null when MyZen has none with this ID.</summary>
    /// <exception cref="MyZenNotConnectedException">No live session.</exception>
    Task<MyZenTimeEntryDto?> GetTimeEntryAsync(long id, CancellationToken cancellationToken);

    /// <summary>The projects whose name matches <paramref name="query"/>, with their customer.</summary>
    /// <exception cref="MyZenNotConnectedException">No live session.</exception>
    Task<IReadOnlyList<MyZenProjectOptionDto>> SearchProjectsAsync(string query, CancellationToken cancellationToken);

    /// <summary>Adds one time entry.</summary>
    /// <exception cref="MyZenNotConnectedException">No live session.</exception>
    /// <exception cref="MyZenRejectedException">MyZen refused the entry.</exception>
    Task CreateTimeEntryAsync(MyZenTimeEntryInput entry, CancellationToken cancellationToken);

    /// <summary>Replaces one time entry; false when MyZen has none with this ID.</summary>
    /// <exception cref="MyZenNotConnectedException">No live session.</exception>
    /// <exception cref="MyZenRejectedException">MyZen refused the change.</exception>
    Task<bool> UpdateTimeEntryAsync(long id, MyZenTimeEntryInput entry, CancellationToken cancellationToken);

    /// <summary>Deletes one time entry; false when MyZen has none with this ID.</summary>
    /// <exception cref="MyZenNotConnectedException">No live session.</exception>
    /// <exception cref="MyZenRejectedException">MyZen refused the delete.</exception>
    Task<bool> DeleteTimeEntryAsync(long id, CancellationToken cancellationToken);
}
