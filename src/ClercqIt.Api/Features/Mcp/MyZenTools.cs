using System.ComponentModel;
using System.Globalization;
using Clercq.It.Application.Features.MyZen;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Clercq.It.Api.Features.Mcp;

/// <summary>
/// The owner's MyZen timesheets as MCP tools (issue #394, docs/myzen.md). Only for
/// <c>MyZen.Owner</c>, and hidden while the <c>MyZen</c> flag is off. Connecting MyZen
/// (CAPTCHA and MFA) stays on the admin tab; the tools use the session it keeps.
/// </summary>
[McpServerToolType]
[Authorize(Policy = Policy)]
public sealed class MyZenTools(McpContent content)
{
    public const string Policy = "MyZenMcp";

    [McpServerTool(Name = "myzen_status", ReadOnly = true)]
    [Description("Whether the MyZen session is Connected, Expired, Connecting or Disconnected. When it is not Connected, the owner must connect MyZen again from the admin page; the tools cannot do that.")]
    public async Task<MyZenStatusDto> StatusAsync(CancellationToken cancellationToken) =>
        await content.SendAsync(new GetMyZenStatusQuery(), cancellationToken);

    [McpServerTool(Name = "list_myzen_projects", ReadOnly = true)]
    [Description("Lists every MyZen project with its customer, dates and status. It has no project or customer IDs for time entries; use search_myzen_projects for those.")]
    public async Task<IReadOnlyList<MyZenProjectDto>> ListProjectsAsync(CancellationToken cancellationToken) =>
        await content.SendAsync(new GetMyZenProjectsQuery(), cancellationToken);

    [McpServerTool(Name = "search_myzen_projects", ReadOnly = true)]
    [Description("Finds MyZen projects by name and returns the projectId and customerId a time entry needs. Absence projects (Feestdag, Vakantie, Ziekte) have no customer.")]
    public async Task<IReadOnlyList<MyZenProjectOptionDto>> SearchProjectsAsync(
        [Description("Part of the project name, 2 to 50 characters")] string query,
        CancellationToken cancellationToken) =>
        await content.SendAsync(new SearchMyZenProjectsQuery(query), cancellationToken);

    [McpServerTool(Name = "list_myzen_time_entries", ReadOnly = true)]
    [Description("Lists the time entries from one day to another, at most 93 days. Durations are hh:mm:ss.")]
    public async Task<IReadOnlyList<MyZenTimeEntryDto>> ListTimeEntriesAsync(
        [Description("First day, yyyy-MM-dd")] DateOnly from,
        [Description("Last day, yyyy-MM-dd")] DateOnly to,
        CancellationToken cancellationToken) =>
        await content.SendAsync(new GetMyZenTimeEntriesQuery(from, to), cancellationToken);

    [McpServerTool(Name = "get_myzen_time_entry", ReadOnly = true)]
    [Description("Gets one time entry by id, with its projectId and customerId.")]
    public async Task<MyZenTimeEntryDto> GetTimeEntryAsync(long id, CancellationToken cancellationToken) =>
        await content.SendAsync(new GetMyZenTimeEntryQuery(id), cancellationToken)
            ?? throw McpContent.NotFound("MyZen time entry", id);

    [McpServerTool(Name = "create_myzen_time_entry")]
    [Description("Adds one time entry in MyZen. Call it once per entry, and only for entries the user asked for. Get projectId and customerId from search_myzen_projects.")]
    public async Task<string> CreateTimeEntryAsync(
        [Description("Day, yyyy-MM-dd")] DateOnly date,
        [Description("Minutes worked, 1 to 1440 (8 hours is 480)")] int durationMinutes,
        [Description("MyZen project id")] long projectId,
        [Description("MyZen customer id of the project; omit for a project without a customer")] long? customerId = null,
        [Description("What was done, max 1000 characters")] string? description = null,
        [Description("Whether the time is already invoiced")] bool invoiced = false,
        CancellationToken cancellationToken = default)
    {
        await content.SendAsync(new CreateMyZenTimeEntryCommand(
            new MyZenTimeEntryInput(date, durationMinutes, projectId, customerId, description, invoiced)), cancellationToken);
        content.Audit("created", "MyZen time entry", date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        return $"Added the time entry on {date:yyyy-MM-dd}.";
    }

    [McpServerTool(Name = "update_myzen_time_entry", Idempotent = true)]
    [Description("Changes one time entry. Only the fields you pass change. When you change projectId, pass that project's customerId too (omit it for a project without a customer).")]
    public async Task<string> UpdateTimeEntryAsync(
        long id,
        [Description("Day, yyyy-MM-dd")] DateOnly? date = null,
        [Description("Minutes worked, 1 to 1440")] int? durationMinutes = null,
        [Description("MyZen project id")] long? projectId = null,
        [Description("MyZen customer id of the project")] long? customerId = null,
        [Description("What was done, max 1000 characters")] string? description = null,
        bool? invoiced = null,
        CancellationToken cancellationToken = default)
    {
        var current = await GetTimeEntryAsync(id, cancellationToken);
        var project = projectId ?? current.ProjectId
            ?? throw new McpException("MyZen did not give this entry's project; pass projectId and customerId.");
        var input = new MyZenTimeEntryInput(
            date ?? current.Date,
            durationMinutes ?? (int)Math.Round(current.Duration.TotalMinutes),
            project,
            // A new project brings its own customer; otherwise keep the entry's.
            projectId is not null ? customerId : customerId ?? current.CustomerId,
            description ?? current.Description,
            invoiced ?? current.Invoiced);

        if (!await content.SendAsync(new UpdateMyZenTimeEntryCommand(id, input), cancellationToken))
        {
            throw McpContent.NotFound("MyZen time entry", id);
        }
        content.Audit("updated", "MyZen time entry", id.ToString(CultureInfo.InvariantCulture));
        return $"Updated time entry {id}.";
    }

    [McpServerTool(Name = "delete_myzen_time_entry", Destructive = true)]
    [Description("Deletes one time entry from MyZen for good. Only delete entries the user asked to remove.")]
    public async Task<string> DeleteTimeEntryAsync(long id, CancellationToken cancellationToken)
    {
        if (!await content.SendAsync(new DeleteMyZenTimeEntryCommand(id), cancellationToken))
        {
            throw McpContent.NotFound("MyZen time entry", id);
        }
        content.Audit("deleted", "MyZen time entry", id.ToString(CultureInfo.InvariantCulture));
        return $"Deleted time entry {id}.";
    }
}
