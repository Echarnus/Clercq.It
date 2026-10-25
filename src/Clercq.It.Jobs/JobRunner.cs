using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Clercq.It.Jobs;

/// <summary>
/// Finds a job by name, runs it in its own DI scope and turns the outcome into the
/// process exit code Scaleway shows for the run. Never logs job output beyond the
/// one-line summary, so jobs stay in charge of what is safe to write.
/// </summary>
public sealed class JobRunner(
    IServiceScopeFactory scopes,
    IConfiguration configuration,
    TimeProvider clock,
    ILogger<JobRunner> logger)
{
    public const int Succeeded = 0;
    public const int Failed = 1;
    public const int UnknownJob = 2;

    /// <summary>Runs the job called <paramref name="name"/>, or lists the jobs when no name is given.</summary>
    /// <returns>0 when the job ran or was switched off, 1 when it failed, 2 for an unknown name.</returns>
    public async Task<int> RunAsync(string? name, CancellationToken cancellationToken)
    {
        using var scope = scopes.CreateScope();
        var jobs = scope.ServiceProvider.GetServices<IScheduledJob>().ToList();

        var duplicate = jobs.GroupBy(j => j.Name, StringComparer.Ordinal).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
        {
            logger.LogError("Two jobs are both called {Job}", duplicate.Key);
            return Failed;
        }

        var job = jobs.SingleOrDefault(j => string.Equals(j.Name, name, StringComparison.Ordinal));
        if (job is null)
        {
            logger.LogError("Unknown job '{Job}'. Known jobs: {Jobs}", name,
                string.Join(", ", jobs.Select(j => j.Name).Order(StringComparer.Ordinal)));
            return UnknownJob;
        }

        if (job.FeatureFlag is { } flag && !IsEnabled(flag))
        {
            logger.LogInformation("Job {Job} is off: feature flag {Flag} is not on", job.Name, flag);
            return Succeeded;
        }

        var started = clock.GetTimestamp();
        try
        {
            var summary = await job.RunAsync(cancellationToken);
            logger.LogInformation("Job {Job} done in {Elapsed:0.0}s: {Summary}", job.Name,
                clock.GetElapsedTime(started).TotalSeconds, summary);
            return Succeeded;
        }
        catch (Exception ex)
        {
            // A cancelled run (Scaleway stopped the job) is a failed run too.
            logger.LogError(ex, "Job {Job} failed after {Elapsed:0.0}s", job.Name,
                clock.GetElapsedTime(started).TotalSeconds);
            return Failed;
        }
    }

    // The same rule as the API's flags: FeatureFlags__<Name>=true switches it on.
    private bool IsEnabled(string flag) =>
        bool.TryParse(configuration[$"FeatureFlags:{flag}"], out var enabled) && enabled;
}
