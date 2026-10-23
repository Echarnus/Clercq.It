using System.Globalization;
using Clercq.It.Application.Features.MyZen;

namespace Clercq.It.Application.Tests.Features.MyZen;

public class MyZenValidatorsTests
{
    [Theory(DisplayName = "A period of up to 93 days, ending on or after its start, is valid")]
    [InlineData("2026-09-01", "2026-09-01", true)]
    [InlineData("2026-09-01", "2026-09-30", true)]
    [InlineData("2026-09-01", "2026-12-02", true)]
    [InlineData("2026-09-01", "2026-12-03", false)]
    [InlineData("2026-09-30", "2026-09-01", false)]
    public void TimeEntriesPeriod_Validated(string from, string to, bool valid)
    {
        // Arrange
        var query = new GetMyZenTimeEntriesQuery(
            DateOnly.Parse(from, CultureInfo.InvariantCulture), DateOnly.Parse(to, CultureInfo.InvariantCulture));

        // Act
        var result = new GetMyZenTimeEntriesQueryValidator().Validate(query);

        // Assert
        result.IsValid.Should().Be(valid);
    }

    [Theory(DisplayName = "A time entry ID must be positive")]
    [InlineData(1, true)]
    [InlineData(0, false)]
    [InlineData(-5, false)]
    public void TimeEntryId_MustBePositive(long id, bool valid)
    {
        new GetMyZenTimeEntryQueryValidator().Validate(new GetMyZenTimeEntryQuery(id)).IsValid.Should().Be(valid);
    }

    [Theory(DisplayName = "The MFA code must be six digits")]
    [InlineData("123456", true)]
    [InlineData("12345", false)]
    [InlineData("1234567", false)]
    [InlineData("12a456", false)]
    [InlineData("", false)]
    public void MfaCode_MustBeSixDigits(string code, bool valid)
    {
        new SubmitMyZenMfaCodeCommandValidator().Validate(new SubmitMyZenMfaCodeCommand(code)).IsValid.Should().Be(valid);
    }

    [Theory(DisplayName = "The CAPTCHA is optional and short")]
    [InlineData(null, true)]
    [InlineData("ab12", true)]
    [InlineData("abcdefghijklmnopqrstu", false)]
    public void Captcha_OptionalAndShort(string? captcha, bool valid)
    {
        new SubmitMyZenLoginCommandValidator().Validate(new SubmitMyZenLoginCommand(captcha)).IsValid.Should().Be(valid);
    }
}
