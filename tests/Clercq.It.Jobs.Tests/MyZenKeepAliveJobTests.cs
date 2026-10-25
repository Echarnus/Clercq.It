using Clercq.It.Application.Features.MyZen;
using Clercq.It.Jobs.Jobs;
using FluentAssertions;
using Moq;

namespace Clercq.It.Jobs.Tests;

public class MyZenKeepAliveJobTests
{
    private readonly Mock<IMyZenGateway> _gateway = new();

    [Fact(DisplayName = "The keep-alive job is called myzen-keepalive and sits behind the MyZen flag")]
    public void Identity()
    {
        var job = new MyZenKeepAliveJob(_gateway.Object);

        job.Name.Should().Be("myzen-keepalive");
        job.FeatureFlag.Should().Be("MyZen");
    }

    [Theory(DisplayName = "The job pings the session and says whether it is alive")]
    [InlineData(true, "MyZen session alive")]
    [InlineData(false, "no live MyZen session")]
    public async Task Run_ReportsSession(bool alive, string summary)
    {
        _gateway.Setup(g => g.KeepAliveAsync(Moq.It.IsAny<CancellationToken>())).ReturnsAsync(alive);

        (await new MyZenKeepAliveJob(_gateway.Object).RunAsync(CancellationToken.None)).Should().Be(summary);
    }

    [Fact(DisplayName = "A MyZen outage fails the run so Scaleway shows it")]
    public async Task Run_MyZenDown_Throws()
    {
        _gateway.Setup(g => g.KeepAliveAsync(Moq.It.IsAny<CancellationToken>()))
            .ThrowsAsync(new MyZenUnavailableException("down"));

        var run = () => new MyZenKeepAliveJob(_gateway.Object).RunAsync(CancellationToken.None);

        await run.Should().ThrowAsync<MyZenUnavailableException>();
    }
}
