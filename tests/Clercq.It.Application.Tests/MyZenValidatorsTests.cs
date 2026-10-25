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

    public static TheoryData<MyZenTimeEntryInput, bool> Entries => new()
    {
        { ValidEntry, true },
        { ValidEntry with { CustomerId = null, Description = null }, true },
        { ValidEntry with { DurationMinutes = 24 * 60 }, true },
        { ValidEntry with { DurationMinutes = 0 }, false },
        { ValidEntry with { DurationMinutes = 24 * 60 + 1 }, false },
        { ValidEntry with { ProjectId = 0 }, false },
        { ValidEntry with { CustomerId = 0 }, false },
        { ValidEntry with { Date = new DateOnly(1999, 12, 31) }, false },
        { ValidEntry with { Description = new string('a', 1001) }, false },
    };

    [Theory(DisplayName = "A time entry needs a project, a duration of up to 24 hours and a short description")]
    [MemberData(nameof(Entries))]
    public void TimeEntry_Validated(MyZenTimeEntryInput entry, bool valid)
    {
        new CreateMyZenTimeEntryCommandValidator().Validate(new CreateMyZenTimeEntryCommand(entry)).IsValid.Should().Be(valid);
        new UpdateMyZenTimeEntryCommandValidator().Validate(new UpdateMyZenTimeEntryCommand(3, entry)).IsValid.Should().Be(valid);
    }

    [Theory(DisplayName = "Updating and deleting need a positive entry ID")]
    [InlineData(1, true)]
    [InlineData(0, false)]
    public void ChangeId_MustBePositive(long id, bool valid)
    {
        new UpdateMyZenTimeEntryCommandValidator().Validate(new UpdateMyZenTimeEntryCommand(id, ValidEntry)).IsValid.Should().Be(valid);
        new DeleteMyZenTimeEntryCommandValidator().Validate(new DeleteMyZenTimeEntryCommand(id)).IsValid.Should().Be(valid);
    }

    [Theory(DisplayName = "A project search is 2 to 50 characters")]
    [InlineData("we", true)]
    [InlineData(" w ", false)]
    [InlineData("", false)]
    public void ProjectSearch_Length(string query, bool valid)
    {
        new SearchMyZenProjectsQueryValidator().Validate(new SearchMyZenProjectsQuery(query)).IsValid.Should().Be(valid);
        new SearchMyZenProjectsQueryValidator().Validate(new SearchMyZenProjectsQuery(new string('a', 51))).IsValid.Should().BeFalse();
    }

    private static readonly MyZenTimeEntryInput ValidEntry = new(new DateOnly(2026, 9, 1), 480, 5, 7, "Work", false);
}
