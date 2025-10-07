using MediatR;
using Clercq.It.Application.Common.Commands;

namespace Clercq.It.Api.Features;

public static class ImagesEndpoints
{
    public static void MapImagesEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/images")
            .WithTags("Images")
            .WithOpenApi();

        group.MapPost("/", async (HttpRequest request, IMediator mediator) =>
        {
            var form = await request.ReadFormAsync();
            
            var imageFile = form.Files["image"];
            if (imageFile == null || imageFile.Length == 0)
            {
                return Results.BadRequest(new { error = "Image file is required" });
            }

            using var imageStream = imageFile.OpenReadStream();
            var command = new UploadImageCommand(
                imageStream,
                imageFile.FileName,
                imageFile.ContentType
            );

            var result = await mediator.Send(command);
            return Results.Ok(result);
        })
        .RequireAuthorization()
        .WithName("UploadImage")
        .WithSummary("Upload an image")
        .WithDescription("Uploads an image to object storage. Returns the image URL. Requires authentication.")
        .DisableAntiforgery();
    }
}
