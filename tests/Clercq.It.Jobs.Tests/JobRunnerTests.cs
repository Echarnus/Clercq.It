using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

namespace Clercq.It.Jobs.Tests;

public class JobRunnerTests
{
    private readonly ListLogger _log = new();

    private sealed class FakeJob(string name, string? flag = null, Func<Task<string>>? run = null) : IScheduledJob
    {
        public int Runs { get; private set; }
        public string Name => name;
        public string? FeatureFlag => flag;

        public Task<string> RunAsync(CancellationToken cancellationToken)
        {
            Runs++;
            return run is null ? Task.FromResult("did it") : run();
        }
    }

    private sealed class ListLogger : ILogger<JobRunner>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) => Entries.Add((logLevel, formatter(state, exception)));
    }

    private JobRunner Runner(IEnumerable<FakeJob> jobs, params (string Key, string Value)[] settings)
    {
        var services = new ServiceCollection();
        foreach (var job in jobs)
        {
            services.AddScoped<IScheduledJob>(_ => job);
        }
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings.ToDictionary(s => s.Key, s => (string?)s.Value))
            .Build();
        return new JobRunner(services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(),
            configuration, new FakeTimeProvider(), _log);
    }

    [Fact(DisplayName = "A known job runs once and its summary is logged")]
    public async Task Run_KnownJob_RunsAndLogsSummary()
    {
        var job = new FakeJob("sync-things");

        var code = await Runner([job]).RunAsync("sync-things", CancellationToken.None);

        code.Should().Be(JobRunner.Succeeded);
        job.Runs.Should().Be(1);
        _log.Entries.Should().ContainSingle(e => e.Level == LogLevel.Information && e.Message.Contains("did it"));
    }

    [Theory(DisplayName = "An unknown or missing job name exits with 2 and lists the known jobs")]
    [InlineData("nope")]
    [InlineData(null)]
    [InlineData("SYNC-THINGS")]
    public async Task Run_UnknownJob_ReturnsUnknownJob(string? name)
    {
        var job = new FakeJob("sync-things");

        var code = await Runner([job]).RunAsync(name, CancellationToken.None);

        code.Should().Be(JobRunner.UnknownJob);
        job.Runs.Should().Be(0);
        _log.Entries.Should().ContainSingle(e => e.Level == LogLevel.Error && e.Message.Contains("sync-things"));
    }

    [Fact(DisplayName = "A failing job exits with 1 and logs the error")]
    public async Task Run_JobThrows_ReturnsFailed()
    {
        var job = new FakeJob("boom", run: () => throw new InvalidOperationException("kaput"));

        var code = await Runner([job]).RunAsync("boom", CancellationToken.None);

        code.Should().Be(JobRunner.Failed);
        _log.Entries.Should().ContainSingle(e => e.Level == LogLevel.Error && e.Message.Contains("boom"));
    }

    [Fact(DisplayName = "A cancelled job counts as failed")]
    public async Task Run_Cancelled_ReturnsFailed()
    {
        var job = new FakeJob("slow", run: () => throw new OperationCanceledException());

        (await Runner([job]).RunAsync("slow", CancellationToken.None)).Should().Be(JobRunner.Failed);
    }

    [Theory(DisplayName = "A job behind a feature flag runs only while the flag is on")]
    [InlineData("true", 1)]
    [InlineData("false", 0)]
    [InlineData("nonsense", 0)]
    [InlineData(null, 0)]
    public async Task Run_FeatureFlag_GatesTheJob(string? value, int runs)
    {
        var job = new FakeJob("gated", flag: "MyFlag");
        (string, string)[] settings = value is null ? [] : [("FeatureFlags:MyFlag", value)];

        var code = await Runner([job], settings).RunAsync("gated", CancellationToken.None);

        code.Should().Be(JobRunner.Succeeded, "a job that is switched off did not fail");
        job.Runs.Should().Be(runs);
    }

    [Fact(DisplayName = "Two jobs with the same name fail the run instead of picking one")]
    public async Task Run_DuplicateNames_ReturnsFailed()
    {
        var first = new FakeJob("twin");
        var second = new FakeJob("twin");

        var code = await Runner([first, second]).RunAsync("twin", CancellationToken.None);

        code.Should().Be(JobRunner.Failed);
        (first.Runs + second.Runs).Should().Be(0);
    }

    [Fact(DisplayName = "AddScheduledJob registers each job in a scope the runner can find")]
    public async Task AddScheduledJob_RegistersJobs()
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build())
            .AddScheduledJobs()
            .AddScheduledJob<RegisteredJob>();
        await using var provider = services.BuildServiceProvider();

        var code = await provider.GetRequiredService<JobRunner>().RunAsync("registered", CancellationToken.None);

        code.Should().Be(JobRunner.Succeeded);
    }

    private sealed class RegisteredJob : IScheduledJob
    {
        public string Name => "registered";

        public Task<string> RunAsync(CancellationToken cancellationToken) => Task.FromResult("ok");
    }
}
