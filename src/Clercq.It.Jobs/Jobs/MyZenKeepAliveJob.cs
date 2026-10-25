using Clercq.It.Application.Features.MyZen;

namespace Clercq.It.Jobs.Jobs;

/// <summary>
/// Opens MyZen's time registration page with the stored session so MyZen keeps it,
/// and marks the session expired when MyZen has dropped it (docs/myzen.md#keep-alive).
/// </summary>
public sealed class MyZenKeepAliveJob(IMyZenGateway gateway) : IScheduledJob
{
    public string Name => "myzen-keepalive";

    public string? FeatureFlag => "MyZen";

    public async Task<string> RunAsync(CancellationToken cancellationToken) =>
        await gateway.KeepAliveAsync(cancellationToken) ? "MyZen session alive" : "no live MyZen session";
}
