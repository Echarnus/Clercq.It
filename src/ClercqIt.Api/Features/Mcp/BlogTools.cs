using System.ComponentModel;
using Clercq.It.Application.Common.DTOs;
using Clercq.It.Application.Features.Blogs.Commands;
using Clercq.It.Application.Features.Blogs.Queries;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Clercq.It.Api.Features.Mcp;

[McpServerToolType]
[Authorize(Policy = "BlogsContributor")]
public sealed class BlogTools(McpContent content)
{
    [McpServerTool(Name = "list_blog_posts", ReadOnly = true)]
    [Description("Lists every blog post on clercq.it.")]
    public async Task<IEnumerable<BlogDto>> ListAsync(CancellationToken cancellationToken) =>
        await content.SendAsync(new GetAllBlogsQuery(), cancellationToken);

    [McpServerTool(Name = "get_blog_post", ReadOnly = true)]
    [Description("Gets one blog post by id.")]
    public async Task<BlogDto> GetAsync(Guid id, CancellationToken cancellationToken) =>
        await content.SendAsync(new GetBlogByIdQuery(id), cancellationToken)
            ?? throw McpContent.NotFound("blog post", id);

    [McpServerTool(Name = "create_blog_post")]
    [Description("Publishes a blog post. The body is Markdown. A cover image is required.")]
    public async Task<BlogDto> CreateAsync(
        [Description("Title, max 200 characters")] string title,
        [Description("Summary, max 500 characters")] string shortDescription,
        [Description("The post in Markdown")] string longDescription,
        [Description("Tags, 1 to 10")] string[] tags,
        [Description("Cover image bytes as base64 (jpg, png, gif or webp, max 5 MB)")] string imageBase64,
        [Description("Image file name with its extension")] string imageFileName,
        CancellationToken cancellationToken = default)
    {
        using var image = McpImage.From(imageBase64, imageFileName)
            ?? throw new McpException("imageBase64 is required for a blog post.");
        var result = await content.SendAsync(new CreateBlogCommand(
            title, shortDescription, longDescription,
            image.Stream, image.FileName, image.ContentType, tags), cancellationToken);
        content.Audit("created", "blog post", result.Id);
        return result;
    }

    [McpServerTool(Name = "update_blog_post", Idempotent = true)]
    [Description("Updates a blog post. Only the fields you pass change; an image replaces the cover.")]
    public async Task<BlogDto> UpdateAsync(
        Guid id,
        string? title = null,
        string? shortDescription = null,
        [Description("The post in Markdown")] string? longDescription = null,
        [Description("Replaces the whole tag list")] string[]? tags = null,
        [Description("Cover image bytes as base64 (jpg, png, gif or webp, max 5 MB)")] string? imageBase64 = null,
        [Description("Image file name with its extension, required with imageBase64")] string? imageFileName = null,
        CancellationToken cancellationToken = default)
    {
        var current = await GetAsync(id, cancellationToken);
        using var image = McpImage.From(imageBase64, imageFileName);
        var result = await content.SendAsync(new UpdateBlogCommand(
            id,
            title ?? current.Title,
            shortDescription ?? current.ShortDescription,
            longDescription ?? current.LongDescription,
            image?.Stream, image?.FileName, image?.ContentType,
            tags ?? current.Tags), cancellationToken)
            ?? throw McpContent.NotFound("blog post", id);
        content.Audit("updated", "blog post", id);
        return result;
    }

    [McpServerTool(Name = "delete_blog_post", Destructive = true)]
    [Description("Deletes a blog post. This cannot be undone.")]
    public async Task<string> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!await content.SendAsync(new DeleteBlogCommand(id), cancellationToken))
        {
            throw McpContent.NotFound("blog post", id);
        }
        content.Audit("deleted", "blog post", id);
        return $"Deleted blog post {id}.";
    }
}
