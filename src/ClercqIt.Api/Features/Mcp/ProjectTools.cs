using System.ComponentModel;
using Clercq.It.Application.Common.DTOs;
using Clercq.It.Application.Features.Projects.Commands;
using Clercq.It.Application.Features.Projects.Queries;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Server;

namespace Clercq.It.Api.Features.Mcp;

[McpServerToolType]
[Authorize(Policy = "ProjectsContributor")]
public sealed class ProjectTools(McpContent content)
{
    [McpServerTool(Name = "list_projects", ReadOnly = true)]
    [Description("Lists every project (past assignments and side projects) on clercq.it.")]
    public async Task<IEnumerable<ProjectDto>> ListAsync(CancellationToken cancellationToken) =>
        await content.SendAsync(new GetAllProjectsQuery(), cancellationToken);

    [McpServerTool(Name = "get_project", ReadOnly = true)]
    [Description("Gets one project by id.")]
    public async Task<ProjectDto> GetAsync(Guid id, CancellationToken cancellationToken) =>
        await content.SendAsync(new GetProjectByIdQuery(id), cancellationToken)
            ?? throw McpContent.NotFound("project", id);

    [McpServerTool(Name = "create_project")]
    [Description("Creates a project. Descriptions are Markdown. Without an image the site shows a placeholder.")]
    public async Task<ProjectDto> CreateAsync(
        [Description("Title, max 200 characters")] string title,
        [Description("Summary shown on cards, max 500 characters")] string shortDescription,
        [Description("Full Markdown description")] string longDescription,
        [Description("Start date, yyyy-MM-dd")] DateOnly startDate,
        [Description("End date, yyyy-MM-dd, on or after the start date")] DateOnly endDate,
        [Description("Skills or technologies, 1 to 20")] string[] skills,
        [Description("Show the project on the homepage")] bool featured = false,
        [Description("Image bytes as base64 (jpg, png, gif or webp, max 5 MB)")] string? imageBase64 = null,
        [Description("Image file name with its extension, required with imageBase64")] string? imageFileName = null,
        CancellationToken cancellationToken = default)
    {
        using var image = McpImage.From(imageBase64, imageFileName);
        var result = await content.SendAsync(new CreateProjectCommand(
            title, shortDescription, longDescription,
            image?.Stream, image?.FileName, image?.ContentType,
            McpContent.Utc(startDate), McpContent.Utc(endDate), featured, skills), cancellationToken);
        content.Audit("created", "project", result.Id);
        return result;
    }

    [McpServerTool(Name = "update_project", Idempotent = true)]
    [Description("Updates a project. Only the fields you pass change; an image replaces the current one.")]
    public async Task<ProjectDto> UpdateAsync(
        Guid id,
        string? title = null,
        string? shortDescription = null,
        [Description("Full Markdown description")] string? longDescription = null,
        [Description("yyyy-MM-dd")] DateOnly? startDate = null,
        [Description("yyyy-MM-dd")] DateOnly? endDate = null,
        [Description("Replaces the whole skills list")] string[]? skills = null,
        bool? featured = null,
        [Description("Image bytes as base64 (jpg, png, gif or webp, max 5 MB)")] string? imageBase64 = null,
        [Description("Image file name with its extension, required with imageBase64")] string? imageFileName = null,
        CancellationToken cancellationToken = default)
    {
        var current = await GetAsync(id, cancellationToken);
        using var image = McpImage.From(imageBase64, imageFileName);
        var result = await content.SendAsync(new UpdateProjectCommand(
            id,
            title ?? current.Title,
            shortDescription ?? current.ShortDescription,
            longDescription ?? current.LongDescription,
            image?.Stream, image?.FileName, image?.ContentType,
            startDate is { } start ? McpContent.Utc(start) : current.StartDate,
            endDate is { } end ? McpContent.Utc(end) : current.EndDate,
            featured ?? current.Featured,
            skills ?? current.Skills), cancellationToken)
            ?? throw McpContent.NotFound("project", id);
        content.Audit("updated", "project", id);
        return result;
    }

    [McpServerTool(Name = "delete_project", Destructive = true)]
    [Description("Deletes a project. This cannot be undone.")]
    public async Task<string> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!await content.SendAsync(new DeleteProjectCommand(id), cancellationToken))
        {
            throw McpContent.NotFound("project", id);
        }
        content.Audit("deleted", "project", id);
        return $"Deleted project {id}.";
    }
}
