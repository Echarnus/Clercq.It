using MediatR;
using Clercq.It.Application.Features.Certifications.Queries;
using Clercq.It.Application.Features.Certifications.Commands;

namespace Clercq.It.Api.Features;

public static class CertificationsEndpoints
{
    public static IEndpointRouteBuilder MapCertificationsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/certifications")
            .WithTags("Certifications")
            .WithOpenApi()
            .RequireRateLimiting("api");

        group.MapGet("/", async (IMediator mediator) =>
        {
            var query = new GetAllCertificationsQuery();
            var result = await mediator.Send(query);
            return Results.Ok(result);
        })
        .WithName("GetAllCertifications")
        .WithSummary("Get all certifications")
        .WithDescription("Retrieves all certifications from the system");

        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var query = new GetCertificationByIdQuery(id);
            var result = await mediator.Send(query);
            return result != null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("GetCertificationById")
        .WithSummary("Get a certification by ID")
        .WithDescription("Retrieves a specific certification by its ID");

        group.MapPost("/", async (HttpRequest request, IMediator mediator) =>
        {
            // Read multipart form data
            var form = await request.ReadFormAsync();

            var title = form["title"].ToString();
            var issuer = form["issuer"].ToString();
            var issueDateString = form["issueDate"].ToString();
            var expiryDateString = form["expiryDate"].ToString();
            var credentialId = form["credentialId"].ToString();
            var credentialUrl = form["credentialUrl"].ToString();
            var description = form["description"].ToString();

            if (!DateTime.TryParse(issueDateString, out var issueDate))
            {
                return Results.BadRequest(new { error = "Invalid issue date format" });
            }

            // Convert to UTC for PostgreSQL timestamp with time zone compatibility
            issueDate = DateTime.SpecifyKind(issueDate, DateTimeKind.Utc);

            DateTime? expiryDate = null;
            if (!string.IsNullOrWhiteSpace(expiryDateString))
            {
                if (!DateTime.TryParse(expiryDateString, out var parsedExpiryDate))
                {
                    return Results.BadRequest(new { error = "Invalid expiry date format" });
                }
                // Convert to UTC for PostgreSQL timestamp with time zone compatibility
                expiryDate = DateTime.SpecifyKind(parsedExpiryDate, DateTimeKind.Utc);
            }

            var imageFile = form.Files["image"];
            if (imageFile == null || imageFile.Length == 0)
            {
                return Results.BadRequest(new { error = "Image file is required" });
            }

            using var imageStream = imageFile.OpenReadStream();
            var command = new CreateCertificationCommand(
                title,
                issuer,
                issueDate,
                expiryDate,
                credentialId,
                credentialUrl,
                description,
                imageStream,
                imageFile.FileName,
                imageFile.ContentType
            );

            var result = await mediator.Send(command);
            return Results.Created($"/api/certifications/{result.Id}", result);
        })
        .RequireAuthorization()
        .WithName("CreateCertification")
        .WithSummary("Create a new certification")
        .WithDescription("Creates a new certification with an image. Requires authentication.")
        .DisableAntiforgery();

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest request, IMediator mediator) =>
        {
            var form = await request.ReadFormAsync();

            var title = form["title"].ToString();
            var issuer = form["issuer"].ToString();
            var issueDateString = form["issueDate"].ToString();
            var expiryDateString = form["expiryDate"].ToString();
            var credentialId = form["credentialId"].ToString();
            var credentialUrl = form["credentialUrl"].ToString();
            var description = form["description"].ToString();

            if (!DateTime.TryParse(issueDateString, out var issueDate))
            {
                return Results.BadRequest(new { error = "Invalid issue date format" });
            }

            // Convert to UTC for PostgreSQL timestamp with time zone compatibility
            issueDate = DateTime.SpecifyKind(issueDate, DateTimeKind.Utc);

            DateTime? expiryDate = null;
            if (!string.IsNullOrWhiteSpace(expiryDateString))
            {
                if (!DateTime.TryParse(expiryDateString, out var parsedExpiryDate))
                {
                    return Results.BadRequest(new { error = "Invalid expiry date format" });
                }
                // Convert to UTC for PostgreSQL timestamp with time zone compatibility
                expiryDate = DateTime.SpecifyKind(parsedExpiryDate, DateTimeKind.Utc);
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

            var command = new UpdateCertificationCommand(
                id,
                title,
                issuer,
                issueDate,
                expiryDate,
                credentialId,
                credentialUrl,
                description,
                imageStream,
                imageFileName,
                imageContentType
            );

            var result = await mediator.Send(command);

            if (imageStream != null)
            {
                await imageStream.DisposeAsync();
            }

            return result != null ? Results.Ok(result) : Results.NotFound();
        })
        .RequireAuthorization()
        .WithName("UpdateCertification")
        .WithSummary("Update a certification")
        .WithDescription("Updates an existing certification. Image is optional - if not provided, the existing image is retained. Requires authentication.")
        .DisableAntiforgery();

        group.MapDelete("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var command = new DeleteCertificationCommand(id);
            var result = await mediator.Send(command);
            return result ? Results.NoContent() : Results.NotFound();
        })
        .RequireAuthorization()
        .WithName("DeleteCertification")
        .WithSummary("Delete a certification")
        .WithDescription("Deletes a certification. Requires authentication.");

        return endpoints;
    }
}
