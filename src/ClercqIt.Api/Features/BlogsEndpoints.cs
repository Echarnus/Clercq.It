using MediatR;
using Clercq.It.Application.Features.Blogs.Queries;

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
    }
}