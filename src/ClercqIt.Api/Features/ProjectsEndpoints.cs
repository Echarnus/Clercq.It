using MediatR;
using Clercq.It.Application.Features.Projects.Queries;
using Clercq.It.Application.Features.Projects.Commands;

namespace Clercq.It.Api.Features;

public static class ProjectsEndpoints
{
    public static IEndpointRouteBuilder MapProjectsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/projects")
            .WithTags("Projects")
            .WithOpenApi()
            .RequireRateLimiting("api");

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

        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var query = new GetProjectByIdQuery(id);
            var result = await mediator.Send(query);

            if (result == null)
            {
                return Results.NotFound();
            }

            return Results.Ok(result);
        })
        .WithName("GetProjectById")
        .WithSummary("Get a project by ID")
        .WithDescription("Retrieves a specific project by its ID");

        group.MapPost("/", async (HttpRequest request, IMediator mediator) =>
        {
            var form = await request.ReadFormAsync();

            var title = form["title"].ToString();
            var shortDescription = form["shortDescription"].ToString();
            var longDescription = form["longDescription"].ToString();
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

            // Convert dates to UTC for PostgreSQL timestamp with time zone compatibility
            startDate = DateTime.SpecifyKind(startDate, DateTimeKind.Utc);
            endDate = DateTime.SpecifyKind(endDate, DateTimeKind.Utc);

            if (!bool.TryParse(featuredString, out var featured))
            {
                featured = false;
            }

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

            var command = new CreateProjectCommand(
                title,
                shortDescription,
                longDescription,
                imageStream,
                imageFileName,
                imageContentType,
                startDate,
                endDate,
                featured,
                skills
            );

            var result = await mediator.Send(command);

            if (imageStream != null)
            {
                await imageStream.DisposeAsync();
            }

            return Results.Created($"/api/projects/{result.Id}", result);
        })
        .RequireAuthorization()
        .WithName("CreateProject")
        .WithSummary("Create a new project")
        .WithDescription("Creates a new project with markdown content and optional image. Requires authentication.")
        .DisableAntiforgery();

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest request, IMediator mediator) =>
        {
            var form = await request.ReadFormAsync();

            var title = form["title"].ToString();
            var shortDescription = form["shortDescription"].ToString();
            var longDescription = form["longDescription"].ToString();
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

            // Convert dates to UTC for PostgreSQL timestamp with time zone compatibility
            startDate = DateTime.SpecifyKind(startDate, DateTimeKind.Utc);
            endDate = DateTime.SpecifyKind(endDate, DateTimeKind.Utc);

            if (!bool.TryParse(featuredString, out var featured))
            {
                featured = false;
            }

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

            var command = new UpdateProjectCommand(
                id,
                title,
                shortDescription,
                longDescription,
                imageStream,
                imageFileName,
                imageContentType,
                startDate,
                endDate,
                featured,
                skills
            );

            var result = await mediator.Send(command);

            if (imageStream != null)
            {
                await imageStream.DisposeAsync();
            }

            return result != null ? Results.Ok(result) : Results.NotFound();
        })
        .RequireAuthorization()
        .WithName("UpdateProject")
        .WithSummary("Update a project")
        .WithDescription("Updates an existing project. Image is optional - if not provided, the existing image is retained. Requires authentication.")
        .DisableAntiforgery();

        group.MapDelete("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var command = new DeleteProjectCommand(id);
            var result = await mediator.Send(command);
            return result ? Results.NoContent() : Results.NotFound();
        })
        .RequireAuthorization()
        .WithName("DeleteProject")
        .WithSummary("Delete a project")
        .WithDescription("Deletes a project. Requires authentication.");

        return endpoints;
    }
}
