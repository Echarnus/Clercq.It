using MediatR;
using Clercq.It.Application.Features.Blogs.Queries;
using Clercq.It.Application.Features.Blogs.Commands;
using Microsoft.AspNetCore.Authorization;

namespace Clercq.It.Api.Features;

public static class BlogsEndpoints
{
    public static void MapBlogsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/blogs")
            .WithTags("Blogs")
            .WithOpenApi();

        group.MapGet("/", async (IMediator mediator) =>
        {
            var query = new GetAllBlogsQuery();
            var result = await mediator.Send(query);
            return Results.Ok(result);
        })
        .WithName("GetAllBlogs")
        .WithSummary("Get all blogs")
        .WithDescription("Retrieves all blogs from the system");

        group.MapPost("/images", async (HttpRequest request, IMediator mediator) =>
        {
            // Read multipart form data
            var form = await request.ReadFormAsync();
            
            var imageFile = form.Files["image"];
            if (imageFile == null || imageFile.Length == 0)
            {
                return Results.BadRequest(new { error = "Image file is required" });
            }

            using var imageStream = imageFile.OpenReadStream();
            var command = new UploadBlogImageCommand(
                imageStream,
                imageFile.FileName,
                imageFile.ContentType
            );

            var result = await mediator.Send(command);
            return Results.Ok(result);
        })
        .RequireAuthorization()
        .WithName("UploadBlogImage")
        .WithSummary("Upload an inline blog image")
        .WithDescription("Uploads an image for use in blog markdown content. Returns the image URL. Requires authentication.")
        .DisableAntiforgery();

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
    }
}
