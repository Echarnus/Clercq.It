using MediatR;
using Clercq.It.Application.Features.Projects.Queries;

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
    }
}