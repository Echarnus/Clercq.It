using System.ComponentModel;
using Clercq.It.Application.Common.DTOs;
using Clercq.It.Application.Features.Certifications.Commands;
using Clercq.It.Application.Features.Certifications.Queries;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Clercq.It.Api.Features.Mcp;

[McpServerToolType]
[Authorize(Policy = "CertificationsContributor")]
public sealed class CertificationTools(McpContent content)
{
    [McpServerTool(Name = "list_certifications", ReadOnly = true)]
    [Description("Lists every certification on clercq.it.")]
    public async Task<IEnumerable<CertificationDto>> ListAsync(CancellationToken cancellationToken) =>
        await content.SendAsync(new GetAllCertificationsQuery(), cancellationToken);

    [McpServerTool(Name = "get_certification", ReadOnly = true)]
    [Description("Gets one certification by id.")]
    public async Task<CertificationDto> GetAsync(Guid id, CancellationToken cancellationToken) =>
        await content.SendAsync(new GetCertificationByIdQuery(id), cancellationToken)
            ?? throw McpContent.NotFound("certification", id);

    [McpServerTool(Name = "create_certification")]
    [Description("Creates a certification. An image (for example the badge or issuer logo) is required.")]
    public async Task<CertificationDto> CreateAsync(
        [Description("Certification name, max 200 characters")] string title,
        [Description("Issuing organisation, max 200 characters")] string issuer,
        [Description("Issue date, yyyy-MM-dd, not in the future")] DateOnly issueDate,
        [Description("Markdown description, max 2000 characters")] string description,
        [Description("Image bytes as base64 (jpg, png, gif or webp, max 5 MB)")] string imageBase64,
        [Description("Image file name with its extension")] string imageFileName,
        [Description("Expiry date, yyyy-MM-dd, after the issue date")] DateOnly? expiryDate = null,
        [Description("Credential id, max 200 characters")] string? credentialId = null,
        [Description("Link to verify the credential, max 500 characters")] string? credentialUrl = null,
        CancellationToken cancellationToken = default)
    {
        using var image = McpImage.From(imageBase64, imageFileName)
            ?? throw new McpException("imageBase64 is required for a certification.");
        var result = await content.SendAsync(new CreateCertificationCommand(
            title, issuer, McpContent.Utc(issueDate),
            expiryDate is { } expiry ? McpContent.Utc(expiry) : null,
            credentialId ?? string.Empty, credentialUrl ?? string.Empty, description,
            image.Stream, image.FileName, image.ContentType), cancellationToken);
        content.Audit("created", "certification", result.Id);
        return result;
    }

    [McpServerTool(Name = "update_certification", Idempotent = true)]
    [Description("Updates a certification. Only the fields you pass change; an image replaces the current one.")]
    public async Task<CertificationDto> UpdateAsync(
        Guid id,
        string? title = null,
        string? issuer = null,
        [Description("yyyy-MM-dd")] DateOnly? issueDate = null,
        [Description("yyyy-MM-dd")] DateOnly? expiryDate = null,
        [Description("Remove the expiry date")] bool clearExpiryDate = false,
        string? credentialId = null,
        string? credentialUrl = null,
        [Description("Markdown description")] string? description = null,
        [Description("Image bytes as base64 (jpg, png, gif or webp, max 5 MB)")] string? imageBase64 = null,
        [Description("Image file name with its extension, required with imageBase64")] string? imageFileName = null,
        CancellationToken cancellationToken = default)
    {
        var current = await GetAsync(id, cancellationToken);
        using var image = McpImage.From(imageBase64, imageFileName);
        var expiry = clearExpiryDate ? null
            : expiryDate is { } date ? McpContent.Utc(date)
            : current.ExpiryDate;
        var result = await content.SendAsync(new UpdateCertificationCommand(
            id,
            title ?? current.Title,
            issuer ?? current.Issuer,
            issueDate is { } issued ? McpContent.Utc(issued) : current.IssueDate,
            expiry,
            credentialId ?? current.CredentialId,
            credentialUrl ?? current.CredentialUrl,
            description ?? current.Description,
            image?.Stream, image?.FileName, image?.ContentType), cancellationToken)
            ?? throw McpContent.NotFound("certification", id);
        content.Audit("updated", "certification", id);
        return result;
    }

    [McpServerTool(Name = "delete_certification", Destructive = true)]
    [Description("Deletes a certification. This cannot be undone.")]
    public async Task<string> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!await content.SendAsync(new DeleteCertificationCommand(id), cancellationToken))
        {
            throw McpContent.NotFound("certification", id);
        }
        content.Audit("deleted", "certification", id);
        return $"Deleted certification {id}.";
    }
}
