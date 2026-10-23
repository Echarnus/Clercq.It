using Clercq.It.Application.Features.MyZen;
using Microsoft.AspNetCore.Diagnostics;

namespace Clercq.It.Api;

/// <summary>
/// MyZen failures become problem responses: 409 when the session must be
/// reconnected, 502 when MyZen cannot be reached or answers unexpectedly.
/// </summary>
public sealed class MyZenExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            MyZenNotConnectedException => (StatusCodes.Status409Conflict, "MyZen is not connected"),
            MyZenUnavailableException => (StatusCodes.Status502BadGateway, "MyZen is unavailable"),
            _ => (0, string.Empty),
        };
        if (status == 0)
        {
            return false;
        }

        await Results.Problem(title: title, detail: exception.Message, statusCode: status).ExecuteAsync(httpContext);
        return true;
    }
}
