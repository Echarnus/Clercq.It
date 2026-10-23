using Clercq.It.Api;
using Clercq.It.Api.FeatureFlags;
using Clercq.It.Application.Features.MyZen;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace ClercqIt.Api.Tests.MyZen;

public class MyZenKeepAliveServiceTests
{
    private readonly Mock<IMyZenGateway> _gateway = new();

    private MyZenKeepAliveService Service(bool flagOn, string? minutes = null, TimeProvider? clock = null)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["FeatureFlags:MyZen"] = flagOn ? "true" : "false",
            ["MyZen:KeepAliveMinutes"] = minutes,
        }).Build();
        var services = new ServiceCollection().AddScoped(_ => _gateway.Object).BuildServiceProvider();
        return new MyZenKeepAliveService(services.GetRequiredService<IServiceScopeFactory>(),
            new ConfigurationFeatureFlags(configuration), configuration, clock ?? TimeProvider.System,
            NullLogger<MyZenKeepAliveService>.Instance);
    }

    [Fact(DisplayName = "The keep-alive does nothing while the MyZen flag is off")]
    public async Task RunOnce_FlagOff_DoesNothing()
    {
        (await Service(flagOn: false).RunOnceAsync(CancellationToken.None)).Should().BeFalse();

        _gateway.VerifyNoOtherCalls();
    }

    [Fact(DisplayName = "The keep-alive touches the session while the flag is on")]
    public async Task RunOnce_FlagOn_TouchesSession()
    {
        // Arrange
        _gateway.Setup(g => g.KeepAliveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act & Assert
        (await Service(flagOn: true).RunOnceAsync(CancellationToken.None)).Should().BeTrue();
    }

    [Fact(DisplayName = "A failing ping is logged and does not stop the service")]
    public async Task RunOnce_Failure_ReturnsFalse()
    {
        // Arrange
        _gateway.Setup(g => g.KeepAliveAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new MyZenUnavailableException("down"));

        // Act & Assert
        (await Service(flagOn: true).RunOnceAsync(CancellationToken.None)).Should().BeFalse();
    }

    [Theory(DisplayName = "The interval comes from MyZen:KeepAliveMinutes, at least one minute")]
    [InlineData(null, 10)]
    [InlineData("3", 3)]
    [InlineData("0", 1)]
    public void Interval_FromConfiguration(string? minutes, int expected)
    {
        Service(flagOn: true, minutes).Interval.Should().Be(TimeSpan.FromMinutes(expected));
    }

    [Fact(DisplayName = "The service pings on every interval until it stops")]
    public async Task Execute_PingsEveryInterval()
    {
        // Arrange
        var clock = new FakeTimeProvider();
        var pinged = new SemaphoreSlim(0);
        _gateway.Setup(g => g.KeepAliveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .Callback(() => pinged.Release());
        var service = Service(flagOn: true, "5", clock);

        // Act
        await service.StartAsync(CancellationToken.None);
        // ExecuteAsync starts on the thread pool, so keep advancing until its timer fires.
        var first = false;
        for (var i = 0; i < 50 && !first; i++)
        {
            clock.Advance(TimeSpan.FromMinutes(5));
            first = await pinged.WaitAsync(TimeSpan.FromMilliseconds(100));
        }
        await service.StopAsync(CancellationToken.None);

        // Assert
        first.Should().BeTrue();
    }
}
