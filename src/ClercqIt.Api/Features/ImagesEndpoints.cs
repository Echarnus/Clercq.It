using MediatR;
using Clercq.It.Application.Features.Blogs.Commands;
using Clercq.It.Application.Features.Projects.Commands;
using Clercq.It.Application.Features.Certifications.Commands;

namespace Clercq.It.Api.Features;

public static class ImagesEndpoints
{
    public static void MapImagesEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/images")
            .WithTags("Images")
            .WithOpenApi();

        group.MapPost("/blogs", async (HttpRequest request, IMediator mediator) =>
        {
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
        .WithSummary("Upload a blog image")
        .WithDescription("Uploads an image for use in blog content. Returns the image URL. Requires authentication.")
        .DisableAntiforgery();

        group.MapPost("/projects", async (HttpRequest request, IMediator mediator) =>
        {
            var form = await request.ReadFormAsync();
            
            var imageFile = form.Files["image"];
            if (imageFile == null || imageFile.Length == 0)
            {
                return Results.BadRequest(new { error = "Image file is required" });
            }

            using var imageStream = imageFile.OpenReadStream();
            var command = new UploadProjectImageCommand(
                imageStream,
                imageFile.FileName,
                imageFile.ContentType
            );

            var result = await mediator.Send(command);
            return Results.Ok(result);
        })
        .RequireAuthorization()
        .WithName("UploadProjectImage")
        .WithSummary("Upload a project image")
        .WithDescription("Uploads an image for use in project content. Returns the image URL. Requires authentication.")
        .DisableAntiforgery();

        group.MapPost("/certifications", async (HttpRequest request, IMediator mediator) =>
        {
            var form = await request.ReadFormAsync();
            
            var imageFile = form.Files["image"];
            if (imageFile == null || imageFile.Length == 0)
            {
                return Results.BadRequest(new { error = "Image file is required" });
            }

            using var imageStream = imageFile.OpenReadStream();
            var command = new UploadCertificationImageCommand(
                imageStream,
                imageFile.FileName,
                imageFile.ContentType
            );

            var result = await mediator.Send(command);
            return Results.Ok(result);
        })
        .RequireAuthorization()
        .WithName("UploadCertificationImage")
        .WithSummary("Upload a certification image")
        .WithDescription("Uploads an image for use in certification content. Returns the image URL. Requires authentication.")
        .DisableAntiforgery();
    }
}
