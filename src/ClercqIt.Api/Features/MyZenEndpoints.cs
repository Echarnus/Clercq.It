using Clercq.It.Api.FeatureFlags;
using Clercq.It.Application.Features.MyZen;
using MediatR;

namespace Clercq.It.Api.Features;

/// <summary>
/// <c>/api/myzen</c>: the owner's own MyZen account (issue #345, docs/myzen.md).
/// Owner only, and hidden (404) until the <c>MyZen</c> feature flag is on.
/// </summary>
public static class MyZenEndpoints
{
    public const string FeatureFlag = "MyZen";
    public const string OwnerPolicy = "MyZenOwner";

    public static IEndpointRouteBuilder MapMyZenEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/myzen")
            .WithTags("MyZen")
            .RequireRateLimiting("api")
            .RequireAuthorization(OwnerPolicy)
            .RequireFeature(FeatureFlag);

        group.MapGet("/session", async (IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new GetMyZenStatusQuery(), ct)))
            .WithName("GetMyZenSession")
            .WithSummary("Get the MyZen session state")
            .WithDescription("Connected, Connecting, Expired or Disconnected, with when it connected and was last seen alive");

        group.MapPost("/session/start", async (IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new StartMyZenLoginCommand(), ct)))
            .WithName("StartMyZenLogin")
            .WithSummary("Start connecting MyZen")
            .WithDescription("Opens the MyZen login page and returns the CAPTCHA to type, if MyZen shows one");

        group.MapPost("/session/login", async (MyZenLoginRequest body, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new SubmitMyZenLoginCommand(body.Captcha), ct)))
            .WithName("SubmitMyZenLogin")
            .WithSummary("Sign in to MyZen")
            .WithDescription("Signs in with the stored credentials and the typed CAPTCHA; the next step may ask for an MFA code");

        group.MapPost("/session/mfa", async (MyZenMfaRequest body, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new SubmitMyZenMfaCodeCommand(body.Code), ct)))
            .WithName("SubmitMyZenMfaCode")
            .WithSummary("Send the MyZen MFA code")
            .WithDescription("Completes the sign-in with the code from the authenticator app");

        group.MapDelete("/session", async (IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new DisconnectMyZenCommand(), ct);
            return Results.NoContent();
        })
            .WithName("DisconnectMyZen")
            .WithSummary("Disconnect MyZen")
            .WithDescription("Forgets the stored MyZen session");

        group.MapGet("/projects", async (IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new GetMyZenProjectsQuery(), ct)))
            .WithName("GetMyZenProjects")
            .WithSummary("List MyZen projects")
            .WithDescription("Every project in MyZen. 409 when the session must be reconnected");

        group.MapGet("/time-entries", async (DateOnly from, DateOnly to, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new GetMyZenTimeEntriesQuery(from, to), ct)))
            .WithName("GetMyZenTimeEntries")
            .WithSummary("List MyZen time entries")
            .WithDescription("Time entries from 'from' to 'to' (yyyy-MM-dd, at most 93 days). 409 when the session must be reconnected");

        group.MapGet("/time-entries/{id:long}", async (long id, IMediator mediator, CancellationToken ct) =>
            await mediator.Send(new GetMyZenTimeEntryQuery(id), ct) is { } entry ? Results.Ok(entry) : Results.NotFound())
            .WithName("GetMyZenTimeEntry")
            .WithSummary("Get a MyZen time entry")
            .WithDescription("One time entry with its project and customer IDs. 409 when the session must be reconnected");

        return endpoints;
    }
}

public record MyZenLoginRequest(string? Captcha);

public record MyZenMfaRequest(string Code);
