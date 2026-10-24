using Clercq.It.Application.Features.Statistics;

namespace Clercq.It.Application.Tests.Features.Statistics;

public class StatisticsHandlersTests
{
    private const string Browser = "Mozilla/5.0 (Macintosh; Intel Mac OS X 14_6) AppleWebKit/605.1.15 Safari/605.1.15";
    private readonly Mock<ISiteStatistics> _statistics = new();

    [Fact(DisplayName = "A page view from a browser is counted under its normalised path")]
    public async Task RecordPageView_Browser_RecordsNormalisedPath()
    {
        // Arrange
        var handler = new RecordPageViewCommandHandler(_statistics.Object);

        // Act
        var counted = await handler.Handle(new RecordPageViewCommand("/Blogs/", " 203.0.113.7 ", Browser), CancellationToken.None);

        // Assert
        counted.Should().BeTrue();
        _statistics.Verify(s => s.RecordPageViewAsync("/blogs", "203.0.113.7", Browser, MoqIt.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory(DisplayName = "Bots, admin pages and requests without an IP or user agent are not counted")]
    [InlineData("/", "203.0.113.7", "Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)")]
    [InlineData("/", "203.0.113.7", "Mozilla/5.0 (X11; Linux x86_64) HeadlessChrome/131.0 Safari/537.36")]
    [InlineData("/", "203.0.113.7", "curl/8.5.0")]
    [InlineData("/admin", "203.0.113.7", Browser)]
    [InlineData("/admin/dashboard", "203.0.113.7", Browser)]
    [InlineData("/api/blogs", "203.0.113.7", Browser)]
    [InlineData("/", null, Browser)]
    [InlineData("/", "203.0.113.7", "")]
    public async Task RecordPageView_NotAVisitor_IsNotCounted(string path, string? ip, string userAgent)
    {
        // Arrange
        var handler = new RecordPageViewCommandHandler(_statistics.Object);

        // Act
        var counted = await handler.Handle(new RecordPageViewCommand(path, ip, userAgent), CancellationToken.None);

        // Assert
        counted.Should().BeFalse();
        _statistics.VerifyNoOtherCalls();
    }

    [Theory(DisplayName = "Paths are lower case without a trailing slash")]
    [InlineData("/", "/")]
    [InlineData("//", "/")]
    [InlineData("/Portfolio/My-Project/", "/portfolio/my-project")]
    [InlineData("/administration", "/administration")]
    public void NormalizePath_ReturnsCanonicalPath(string path, string expected)
    {
        // Act
        var result = RecordPageViewCommandHandler.NormalizePath(path);

        // Assert
        result.Should().Be(expected);
    }

    [Fact(DisplayName = "A page that only starts like /admin is counted")]
    public async Task RecordPageView_AdminLookalikePath_IsCounted()
    {
        // Arrange
        var handler = new RecordPageViewCommandHandler(_statistics.Object);

        // Act
        var counted = await handler.Handle(new RecordPageViewCommand("/administration", "203.0.113.7", Browser), CancellationToken.None);

        // Assert
        counted.Should().BeTrue();
    }

    [Fact(DisplayName = "The statistics query fills quiet days with zero and sums the totals")]
    public async Task GetStatistics_SparseDays_FillsAndSums()
    {
        // Arrange
        var today = new DateOnly(2026, 10, 24);
        var clock = Clock(today);
        _statistics.Setup(s => s.GetAsync(new DateOnly(2026, 10, 18), today, MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(new StatisticsData(
                [new(new DateOnly(2026, 10, 20), 10, 6, 4), new(today, 5, 3, 2)],
                [new("/", 9, 5), new("/blogs", 6, 4)]));
        var handler = new GetSiteStatisticsQueryHandler(_statistics.Object, clock);

        // Act
        var result = await handler.Handle(new GetSiteStatisticsQuery(7), CancellationToken.None);

        // Assert
        result.From.Should().Be(new DateOnly(2026, 10, 18));
        result.To.Should().Be(today);
        result.Days.Should().HaveCount(7);
        result.Days.Select(d => d.Date).Should().BeInAscendingOrder();
        result.Days.Single(d => d.Date == new DateOnly(2026, 10, 19)).PageViews.Should().Be(0);
        result.PageViews.Should().Be(15);
        result.UniquePageViews.Should().Be(9);
        result.Visitors.Should().Be(6);
        result.TopPages.Select(p => p.Path).Should().Equal("/", "/blogs");
    }

    [Fact(DisplayName = "The statistics query keeps only the ten most viewed pages")]
    public async Task GetStatistics_ManyPages_ReturnsTopTen()
    {
        // Arrange
        var today = new DateOnly(2026, 10, 24);
        var pages = Enumerable.Range(1, 15).Select(i => new PageStatisticsDto($"/p{i}", i, i)).ToList();
        _statistics.Setup(s => s.GetAsync(MoqIt.IsAny<DateOnly>(), MoqIt.IsAny<DateOnly>(), MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(new StatisticsData([], pages));
        var handler = new GetSiteStatisticsQueryHandler(_statistics.Object, Clock(today));

        // Act
        var result = await handler.Handle(new GetSiteStatisticsQuery(30), CancellationToken.None);

        // Assert
        result.TopPages.Should().HaveCount(GetSiteStatisticsQueryHandler.TopPageCount);
        result.TopPages[0].Path.Should().Be("/p15");
        result.PageViews.Should().Be(0);
    }

    [Fact(DisplayName = "The statistics query passes a store failure on")]
    public async Task GetStatistics_StoreFails_Throws()
    {
        // Arrange
        _statistics.Setup(s => s.GetAsync(MoqIt.IsAny<DateOnly>(), MoqIt.IsAny<DateOnly>(), MoqIt.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("database down"));
        var handler = new GetSiteStatisticsQueryHandler(_statistics.Object, Clock(new DateOnly(2026, 10, 24)));

        // Act
        var act = () => handler.Handle(new GetSiteStatisticsQuery(7), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    private static TimeProvider Clock(DateOnly today)
    {
        var clock = new Mock<TimeProvider>();
        clock.Setup(c => c.GetUtcNow()).Returns(new DateTimeOffset(today.ToDateTime(new TimeOnly(15, 0)), TimeSpan.Zero));
        return clock.Object;
    }
}
