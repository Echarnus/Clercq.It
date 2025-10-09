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

            DateTime? expiryDate = null;
            if (!string.IsNullOrWhiteSpace(expiryDateString))
            {
                if (!DateTime.TryParse(expiryDateString, out var parsedExpiryDate))
                {
                    return Results.BadRequest(new { error = "Invalid expiry date format" });
                }
                expiryDate = parsedExpiryDate;
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

        return endpoints;
    }
}
