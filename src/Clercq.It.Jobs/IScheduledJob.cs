namespace Clercq.It.Jobs;

/// <summary>
/// One piece of timed work, run by <see cref="JobRunner"/> as
/// <c>dotnet Clercq.It.Jobs.dll &lt;name&gt;</c>. Scaleway starts that command on a cron
/// schedule from infra/terraform/jobs.tf. See docs/jobs.md.
/// </summary>
public interface IScheduledJob
{
    /// <summary>
    /// The kebab-case name used on the command line. It is also the key of the job in
    /// <c>scheduled_jobs</c> in infra/terraform/jobs.tf.
    /// </summary>
    string Name { get; }

    /// <summary>The feature flag that switches the job on; null for a job that always runs.</summary>
    string? FeatureFlag => null;

    /// <summary>Runs the job once; an exception fails the run.</summary>
    /// <returns>One line that says what happened, for the run's log.</returns>
    Task<string> RunAsync(CancellationToken cancellationToken);
}
