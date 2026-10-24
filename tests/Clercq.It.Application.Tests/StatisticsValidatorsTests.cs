using Clercq.It.Application.Features.Statistics;

namespace Clercq.It.Application.Tests.Features.Statistics;

public class StatisticsValidatorsTests
{
    [Theory(DisplayName = "A page view path must be a plain URL path")]
    [InlineData("/", true)]
    [InlineData("/portfolio/my-project", true)]
    [InlineData("/blogs/caf%C3%A9", true)]
    [InlineData("", false)]
    [InlineData("blogs", false)]
    [InlineData("/blogs?utm_source=x", false)]
    [InlineData("/<script>", false)]
    [InlineData("https://example.com/", false)]
    public void RecordPageView_Path_IsValidated(string path, bool valid)
    {
        // Act
        var result = new RecordPageViewCommandValidator().Validate(new RecordPageViewCommand(path, null, null));

        // Assert
        result.IsValid.Should().Be(valid);
    }

    [Fact(DisplayName = "A page view path longer than 200 characters is rejected")]
    public void RecordPageView_LongPath_IsRejected()
    {
        // Act
        var result = new RecordPageViewCommandValidator().Validate(new RecordPageViewCommand("/" + new string('a', 200), null, null));

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Theory(DisplayName = "The statistics period is 1 to 400 days")]
    [InlineData(1, true)]
    [InlineData(90, true)]
    [InlineData(400, true)]
    [InlineData(0, false)]
    [InlineData(401, false)]
    public void GetStatistics_Days_IsValidated(int days, bool valid)
    {
        // Act
        var result = new GetSiteStatisticsQueryValidator().Validate(new GetSiteStatisticsQuery(days));

        // Assert
        result.IsValid.Should().Be(valid);
    }
}
