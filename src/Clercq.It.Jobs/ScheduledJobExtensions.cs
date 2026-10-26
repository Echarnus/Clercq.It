using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Clercq.It.Jobs;

public static class ScheduledJobExtensions
{
    /// <summary>Adds the runner. Register each job with <see cref="AddScheduledJob{TJob}"/>.</summary>
    public static IServiceCollection AddScheduledJobs(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<JobRunner>();
        return services;
    }

    /// <summary>Makes <typeparamref name="TJob"/> runnable by its <see cref="IScheduledJob.Name"/>.</summary>
    public static IServiceCollection AddScheduledJob<TJob>(this IServiceCollection services)
        where TJob : class, IScheduledJob =>
        services.AddScoped<IScheduledJob, TJob>();
}
