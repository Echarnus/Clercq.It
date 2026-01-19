using MediatR;
using Clercq.It.Application.Features.Blogs.Queries;
using Clercq.It.Application.Features.Blogs.Commands;
using Microsoft.AspNetCore.Authorization;

namespace Clercq.It.Api.Features;

public static class BlogsEndpoints
{
    public static IEndpointRouteBuilder MapBlogsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/blogs")
            .WithTags("Blogs")
            .WithOpenApi()
            .RequireRateLimiting("api");

        group.MapGet("/", async (IMediator mediator) =>
        {
            var query = new GetAllBlogsQuery();
            var result = await mediator.Send(query);
            return Results.Ok(result);
        })
        .WithName("GetAllBlogs")
        .WithSummary("Get all blogs")
        .WithDescription("Retrieves all blogs from the system");

        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var query = new GetBlogByIdQuery(id);
            var result = await mediator.Send(query);
            return result != null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("GetBlogById")
        .WithSummary("Get a blog by ID")
        .WithDescription("Retrieves a specific blog by its ID");

        group.MapPost("/", async (HttpRequest request, IMediator mediator) =>
        {
            // Read multipart form data
            var form = await request.ReadFormAsync();

            var shortDescription = form["shortDescription"].ToString();
            var longDescription = form["longDescription"].ToString();
            var tagsString = form["tags"].ToString();
            var tags = tagsString.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .ToArray();

            var imageFile = form.Files["image"];
            if (imageFile == null || imageFile.Length == 0)
            {
                return Results.BadRequest(new { error = "Image file is required" });
            }

            using var imageStream = imageFile.OpenReadStream();
            var command = new CreateBlogCommand(
                shortDescription,
                longDescription,
                imageStream,
                imageFile.FileName,
                imageFile.ContentType,
                tags
            );

            var result = await mediator.Send(command);
            return Results.Created($"/api/blogs/{result.Id}", result);
        })
        .RequireAuthorization()
        .WithName("CreateBlog")
        .WithSummary("Create a new blog")
        .WithDescription("Creates a new blog post with markdown content and an image. Requires authentication.")
        .DisableAntiforgery();

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest request, IMediator mediator) =>
        {
            var form = await request.ReadFormAsync();

            var shortDescription = form["shortDescription"].ToString();
            var longDescription = form["longDescription"].ToString();
            var tagsString = form["tags"].ToString();
            var tags = tagsString.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .ToArray();

            var imageFile = form.Files["image"];
            Stream? imageStream = null;
            string? imageFileName = null;
            string? imageContentType = null;

            if (imageFile != null && imageFile.Length > 0)
            {
                imageStream = imageFile.OpenReadStream();
                imageFileName = imageFile.FileName;
                imageContentType = imageFile.ContentType;
            }

            var command = new UpdateBlogCommand(
                id,
                shortDescription,
                longDescription,
                imageStream,
                imageFileName,
                imageContentType,
                tags
            );

            var result = await mediator.Send(command);

            if (imageStream != null)
            {
                await imageStream.DisposeAsync();
            }

            return result != null ? Results.Ok(result) : Results.NotFound();
        })
        .RequireAuthorization()
        .WithName("UpdateBlog")
        .WithSummary("Update a blog")
        .WithDescription("Updates an existing blog post. Image is optional - if not provided, the existing image is retained. Requires authentication.")
        .DisableAntiforgery();

        group.MapDelete("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var command = new DeleteBlogCommand(id);
            var result = await mediator.Send(command);
            return result ? Results.NoContent() : Results.NotFound();
        })
        .RequireAuthorization()
        .WithName("DeleteBlog")
        .WithSummary("Delete a blog")
        .WithDescription("Deletes a blog post. Requires authentication.");

        return endpoints;
    }
}
