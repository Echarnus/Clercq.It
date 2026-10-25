using Clercq.It.Infrastructure;
using Clercq.It.Jobs.Jobs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Clercq.It.Jobs;

/// <summary>
/// The composition root: configuration from the environment (and command line), the
/// infrastructure services the jobs use, and every job. Add each new job here, with a
/// matching entry in <c>scheduled_jobs</c> in infra/terraform/jobs.tf (docs/jobs.md).
/// </summary>
public static class JobsHost
{
    public static IHost Create(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);

        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddScheduledJobs();

        builder.Services.AddScheduledJob<MyZenKeepAliveJob>();

        return builder.Build();
    }
}
