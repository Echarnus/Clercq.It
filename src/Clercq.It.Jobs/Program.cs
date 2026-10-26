using System.Runtime.InteropServices;
using Clercq.It.Jobs;
using Microsoft.Extensions.DependencyInjection;

// Runs one scheduled job and exits (docs/jobs.md).
// Usage: dotnet Clercq.It.Jobs.dll <job-name>
// Config (environment): ConnectionStrings__DefaultConnection, Database__CaCertificate,
// FeatureFlags__<Name> and whatever the job needs, such as MyZen__SessionKey.
using var host = JobsHost.Create(args);

using var stop = new CancellationTokenSource();
// Scaleway stops a job that runs past its timeout with SIGTERM.
using var sigterm = PosixSignalRegistration.Create(PosixSignal.SIGTERM, context =>
{
    context.Cancel = true;
    stop.Cancel();
});

return await host.Services.GetRequiredService<JobRunner>().RunAsync(args.FirstOrDefault(), stop.Token);
