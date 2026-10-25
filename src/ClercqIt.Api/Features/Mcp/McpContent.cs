using System.Security.Claims;
using Clercq.It.Application.Features.MyZen;
using FluentValidation;
using MediatR;
using ModelContextProtocol;

namespace Clercq.It.Api.Features.Mcp;

/// <summary>
/// Shared plumbing for the content MCP tools: sends commands through MediatR (so the
/// same validators and handlers run as for the admin UI), turns validation failures
/// into tool errors the model can read, and logs every write with the caller.
/// </summary>
public sealed class McpContent(IMediator mediator, IHttpContextAccessor httpContext, ILogger<McpContent> logger)
{
    public async Task SendAsync(IRequest request, CancellationToken cancellationToken) =>
        await SendAsync<Unit>(async () =>
        {
            await mediator.Send(request, cancellationToken);
            return Unit.Value;
        });

    public Task<T> SendAsync<T>(IRequest<T> request, CancellationToken cancellationToken) =>
        SendAsync(() => mediator.Send(request, cancellationToken));

    private static async Task<T> SendAsync<T>(Func<Task<T>> send)
    {
        try
        {
            return await send();
        }
        catch (ValidationException ex)
        {
            var messages = ex.Errors.Select(e => e.ErrorMessage).Distinct();
            throw new McpException("Validation failed: " + string.Join("; ", messages));
        }
        catch (Exception ex) when (ex is MyZenNotConnectedException or MyZenUnavailableException or MyZenRejectedException)
        {
            // MyZen's own messages say what to do (reconnect, MyZen's reason) and hold no secrets.
            throw new McpException(ex.Message);
        }
    }

    public void Audit(string action, string type, Guid id) => Audit(action, type, id.ToString());

    public void Audit(string action, string type, string id)
    {
        var user = httpContext.HttpContext?.User ?? new ClaimsPrincipal();
        // Service accounts have no username, so fall back to the client id (azp).
        var caller = user.FindFirstValue("preferred_username") ?? user.FindFirstValue("azp") ?? "unknown";
        logger.LogInformation("MCP {Action} {Type} {Id} by {Caller}", action, type, id, caller);
    }

    public static DateTime Utc(DateOnly date) => date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

    public static McpException NotFound(string type, Guid id) => new($"No {type} with id {id}.");

    public static McpException NotFound(string type, long id) => new($"No {type} with id {id}.");
}

/// <summary>An image passed to a tool as base64, decoded and checked before upload.</summary>
public sealed record McpImage(Stream Stream, string FileName, string ContentType) : IDisposable
{
    // Same formats the admin UI accepts (see the command validators).
    private static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".gif"] = "image/gif",
        [".webp"] = "image/webp",
    };

    public const int MaxBytes = 5 * 1024 * 1024;

    public static McpImage? From(string? base64, string? fileName)
    {
        if (string.IsNullOrWhiteSpace(base64))
        {
            return null;
        }
        if (string.IsNullOrWhiteSpace(fileName) || !ContentTypes.TryGetValue(Path.GetExtension(fileName), out var contentType))
        {
            throw new McpException("imageFileName must end in .jpg, .jpeg, .png, .gif or .webp.");
        }

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(base64);
        }
        catch (FormatException)
        {
            throw new McpException("imageBase64 is not valid base64.");
        }
        if (bytes.Length > MaxBytes)
        {
            throw new McpException($"The image is larger than {MaxBytes / 1024 / 1024} MB.");
        }

        return new McpImage(new MemoryStream(bytes), Path.GetFileName(fileName), contentType);
    }

    public void Dispose() => Stream.Dispose();
}
