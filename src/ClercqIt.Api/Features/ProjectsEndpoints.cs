using MediatR;
using Clercq.It.Application.Features.Projects.Queries;
using Clercq.It.Application.Features.Projects.Commands;

namespace Clercq.It.Api.Features;

public static class ProjectsEndpoints
{
    public static void MapProjectsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/projects")
            .WithTags("Projects")
            .WithOpenApi();

        group.MapGet("/", async (IMediator mediator) =>
        {
            var query = new GetAllProjectsQuery();
            var result = await mediator.Send(query);
            return Results.Ok(result);
        })
        .WithName("GetAllProjects")
        .WithSummary("Get all projects")
        .WithDescription("Retrieves all projects from the system");

        group.MapGet("/featured", async (IMediator mediator) =>
        {
            var query = new GetFeaturedProjectsQuery();
            var result = await mediator.Send(query);
            return Results.Ok(result);
        })
        .WithName("GetFeaturedProjects")
        .WithSummary("Get featured projects")
        .WithDescription("Retrieves all featured projects from the system");

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
        .WithDescription("Uploads an image for a project. Returns the image URL. Requires authentication.")
        .DisableAntiforgery();

        group.MapPost("/", async (HttpRequest request, IMediator mediator) =>
        {
            // Read multipart form data
            var form = await request.ReadFormAsync();
            
            var title = form["title"].ToString();
            var shortDescription = form["shortDescription"].ToString();
            var longDescription = form["longDescription"].ToString();
            var imageUrl = form["imageUrl"].ToString();
            var startDateString = form["startDate"].ToString();
            var endDateString = form["endDate"].ToString();
            var featuredString = form["featured"].ToString();
            var skillsString = form["skills"].ToString();
            
            var skills = skillsString.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .ToArray();

            if (!DateTime.TryParse(startDateString, out var startDate))
            {
                return Results.BadRequest(new { error = "Invalid start date format" });
            }

            if (!DateTime.TryParse(endDateString, out var endDate))
            {
                return Results.BadRequest(new { error = "Invalid end date format" });
            }

            if (!bool.TryParse(featuredString, out var featured))
            {
                featured = false; // Default to false if not provided or invalid
            }

            var command = new CreateProjectCommand(
                title,
                shortDescription,
                longDescription,
                imageUrl,
                startDate,
                endDate,
                featured,
                skills
            );

            var result = await mediator.Send(command);
            return Results.Created($"/api/projects/{result.Id}", result);
        })
        .RequireAuthorization()
        .WithName("CreateProject")
        .WithSummary("Create a new project")
        .WithDescription("Creates a new project with markdown content. Image must be uploaded separately via /api/projects/images. Requires authentication.")
        .DisableAntiforgery();
    }
}