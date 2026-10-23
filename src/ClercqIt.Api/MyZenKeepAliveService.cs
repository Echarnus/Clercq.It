using Clercq.It.Api.FeatureFlags;
using Clercq.It.Api.Features;
using Clercq.It.Application.Features.MyZen;

namespace Clercq.It.Api;

/// <summary>
/// Touches a connected MyZen session every <c>MyZen:KeepAliveMinutes</c> so MyZen
/// keeps it, and marks it expired when MyZen has dropped it. Runs only while the
/// container runs; see docs/myzen.md#keep-alive.
/// </summary>
public sealed class MyZenKeepAliveService(
    IServiceScopeFactory scopes,
    IFeatureFlags flags,
    IConfiguration configuration,
    TimeProvider clock,
    ILogger<MyZenKeepAliveService> logger) : BackgroundService
{
    public TimeSpan Interval =>
        TimeSpan.FromMinutes(Math.Max(1, configuration.GetValue("MyZen:KeepAliveMinutes", 10)));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval, clock);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunOnceAsync(stoppingToken);
        }
    }

    /// <summary>One keep-alive tick: touches the session when the MyZen flag is on.</summary>
    /// <returns>Whether a live session was touched.</returns>
    public async Task<bool> RunOnceAsync(CancellationToken cancellationToken)
    {
        if (!flags.IsEnabled(MyZenEndpoints.FeatureFlag))
        {
            return false;
        }

        try
        {
            using var scope = scopes.CreateScope();
            return await scope.ServiceProvider.GetRequiredService<IMyZenGateway>().KeepAliveAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // A failed ping must not stop the service; the next tick tries again.
            logger.LogWarning(ex, "MyZen keep-alive failed");
            return false;
        }
    }
}
