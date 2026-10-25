using Clercq.It.Application.Features.MyZen;
using Clercq.It.Infrastructure.MyZen;
using FluentAssertions;

namespace ClercqIt.Api.Tests.MyZen;

public class MyZenHtmlTests
{
    [Fact(DisplayName = "The login page gives its tokens and no CAPTCHA while it is hidden")]
    public void ParseLoginPage_HiddenCaptcha_TokensOnly()
    {
        // Act
        var page = MyZenHtml.ParseLoginPage(MyZenPages.Login());

        // Assert
        page.Should().Be(new MyZenLoginPage("form-token", "header-token", "captcha-check", null, null));
    }

    [Fact(DisplayName = "The login page gives the CAPTCHA image and error when shown")]
    public void ParseLoginPage_ShownCaptcha_ReturnsImageAndError()
    {
        // Act
        var page = MyZenHtml.ParseLoginPage(MyZenPages.Login(captcha: true, error: "  Wrong   password "));

        // Assert
        page.CaptchaImage.Should().Be(MyZenPages.CaptchaImage);
        page.Error.Should().Be("Wrong password");
    }

    [Fact(DisplayName = "A long MyZen error is cut to 300 characters")]
    public void ParseLoginPage_LongError_Truncated()
    {
        MyZenHtml.ParseLoginPage(MyZenPages.Login(error: new string('x', 400))).Error.Should().HaveLength(300);
    }

    [Fact(DisplayName = "Without the token config the form token comes from the hidden input")]
    public void ParseLoginPage_NoConfig_FallsBackToHiddenInput()
    {
        // Act
        var page = MyZenHtml.ParseLoginPage("""<form><input name="__RequestVerificationToken" value="fallback" /></form>""");

        // Assert
        page.Should().Be(new MyZenLoginPage("fallback", null, null, null, null));
    }

    [Fact(DisplayName = "A signed-in page gives its request token and filter values")]
    public void ParseIndex_TokenAndFilter()
    {
        // Act
        var token = MyZenHtml.ParseRequestVerificationToken(MyZenPages.TimeRegistrationIndex);
        var filter = MyZenHtml.ParseTimeEntryFilter(MyZenPages.TimeRegistrationIndex);

        // Assert
        token.Should().Be("page-token");
        filter.Should().Contain(KeyValuePair.Create("F.UserID", "4242"))
            .And.Contain(KeyValuePair.Create("F.SortDescending", "True"));
        MyZenHtml.ParseTimeEntryFilter("<html></html>").Should().BeEmpty();
    }

    [Fact(DisplayName = "The request token comes from the layout config even without a hidden input (#448)")]
    public void ParseRequestVerificationToken_ConfigOnly()
    {
        // Arrange: MyZen serves no hidden token input; its scripts add them in the browser.
        MyZenPages.TimeRegistrationIndex.Should().NotContain("<input name=\"__RequestVerificationToken\"");

        // Act
        var token = MyZenHtml.ParseRequestVerificationToken(MyZenPages.TimeRegistrationIndex);

        // Assert
        token.Should().Be("page-token");
    }

    [Theory(DisplayName = "Without a usable layout config the request token comes from a hidden input")]
    [InlineData("")]
    [InlineData("""<script type="application/json" id="layoutmainconfig">not json</script>""")]
    [InlineData("""<script type="application/json" id="layoutmainconfig">{"rawcsrfToken":""}</script>""")]
    public void ParseRequestVerificationToken_FallsBackToHiddenInput(string config)
    {
        // Act
        var token = MyZenHtml.ParseRequestVerificationToken(
            $"""<html><body>{config}<form><input name="__RequestVerificationToken" value="fallback" /></form></body></html>""");

        // Assert
        token.Should().Be("fallback");
    }

    [Fact(DisplayName = "A page without any request token gives none")]
    public void ParseRequestVerificationToken_None()
    {
        MyZenHtml.ParseRequestVerificationToken("<html><body><form id=\"gridform\"></form></body></html>").Should().BeNull();
    }

    [Fact(DisplayName = "The time entry list gives each row and whether a next page exists")]
    public void ParseTimeEntryList_RowsAndPaging()
    {
        // Arrange
        var html = MyZenPages.EntryList(
            [
                MyZenPages.EntryRow(101, "01/09/2026", "Planning", "Acme NV", "Acme Website", 28800, invoiced: true),
                MyZenPages.EntryRow(102, "02/09/2026", "Build", "Acme NV", "Acme Website", 5400, invoiced: false),
                "<tr><td>no id</td></tr>",
                MyZenPages.EntryRow(103, "not a date", "Broken", "Acme NV", "Acme Website", 60, invoiced: false),
            ],
            0, 1);

        // Act
        var page0 = MyZenHtml.ParseTimeEntryList(html, 0);
        var page1 = MyZenHtml.ParseTimeEntryList(html, 1);

        // Assert
        page0.HasNextPage.Should().BeTrue();
        page1.HasNextPage.Should().BeFalse();
        page0.Entries.Should().BeEquivalentTo(new[]
        {
            new Clercq.It.Application.Features.MyZen.MyZenTimeEntryDto(101, new DateOnly(2026, 9, 1), "Planning", null, "Acme Website", null, "Acme NV", TimeSpan.FromHours(8), true),
            new Clercq.It.Application.Features.MyZen.MyZenTimeEntryDto(102, new DateOnly(2026, 9, 2), "Build", null, "Acme Website", null, "Acme NV", TimeSpan.FromMinutes(90), false),
        });
    }

    [Fact(DisplayName = "The edit form gives the entry with its project and customer IDs")]
    public void ParseTimeEntryDetails_FullEntry()
    {
        // Act
        var entry = MyZenHtml.ParseTimeEntryDetails(MyZenPages.EntryDetails(101));

        // Assert
        entry.Should().Be(new Clercq.It.Application.Features.MyZen.MyZenTimeEntryDto(
            101, new DateOnly(2026, 9, 15), "Sprint review", 555, "Acme Website", 777, "Acme NV", new TimeSpan(7, 30, 0), true));
    }

    [Theory(DisplayName = "The edit form duration may carry seconds; a bad one counts as zero")]
    [InlineData("01:02:03", 1, 2, 3)]
    [InlineData("bad", 0, 0, 0)]
    [InlineData("1:xx", 0, 0, 0)]
    public void ParseTimeEntryDetails_Durations(string duration, int h, int m, int s)
    {
        MyZenHtml.ParseTimeEntryDetails(MyZenPages.EntryDetails(1, duration: duration, invoiced: false))!
            .Duration.Should().Be(new TimeSpan(h, m, s));
    }

    [Theory(DisplayName = "An edit form without an entry or date gives nothing")]
    [InlineData("<form></form>")]
    [InlineData("""<form><input name="EditTimeRegistration.TimeRegistrationID" value="5" /><input name="EditTimeRegistration.Date" value="" /></form>""")]
    public void ParseTimeEntryDetails_Incomplete_ReturnsNull(string html)
    {
        MyZenHtml.ParseTimeEntryDetails(html).Should().BeNull();
    }

    [Fact(DisplayName = "The project list gives each project and whether a next page exists")]
    public void ParseProjectList_RowsAndPaging()
    {
        // Act
        var (projects, hasNext) = MyZenHtml.ParseProjectList(MyZenPages.ProjectList(0, 1), 0);
        var (_, hasNextAfterLast) = MyZenHtml.ParseProjectList(MyZenPages.ProjectList(0, 1), 1);

        // Assert
        projects.Should().ContainSingle().Which.Should().Be(
            new MyZenProjectDto(555, "Acme Website", "Acme NV", "01/01/2026", null, "Bezig"));
        hasNext.Should().BeTrue();
        hasNextAfterLast.Should().BeFalse();
    }

    [Fact(DisplayName = "A short project row fills missing cells with empty text")]
    public void ParseProjectList_ShortRow_EmptyCells()
    {
        var (projects, _) = MyZenHtml.ParseProjectList("""<table><tbody><tr id="ProjectID-9"><td>Solo</td></tr></tbody></table>""", 0);

        projects.Should().ContainSingle().Which.Should().Be(
            new MyZenProjectDto(9, "Solo", "", null, null, ""));
    }

    [Fact(DisplayName = "The project lookup gives each project with its customer")]
    public void ParseProjectSuggestions_ReadsCustomer()
    {
        // Act
        var options = MyZenHtml.ParseProjectSuggestions(MyZenPages.ProjectSuggestions);

        // Assert
        options.Should().Equal(
            new MyZenProjectOptionDto(20788, "Acme Website", 777, "Acme NV"),
            new MyZenProjectOptionDto(23200, "Vakantie", null, ""));
    }

    [Theory(DisplayName = "An unexpected project lookup answer gives no projects")]
    [InlineData("not json")]
    [InlineData("{}")]
    [InlineData("""[{"label":"No id"},{"value":"x","label":"Bad id"},3]""")]
    public void ParseProjectSuggestions_Unexpected_Empty(string body)
    {
        MyZenHtml.ParseProjectSuggestions(body).Should().BeEmpty();
    }

    [Theory(DisplayName = "A save or delete answer without an error counts as accepted")]
    [InlineData("")]
    [InlineData("""{"Success":true}""")]
    [InlineData("{broken")]
    [InlineData("""<form><span class="field-validation-valid"></span><div class="alert alert-danger d-none">hidden</div></form>""")]
    public void ParseChangeError_Accepted_Null(string body)
    {
        MyZenHtml.ParseChangeError(body).Should().BeNull();
    }

    [Theory(DisplayName = "A save or delete answer with an error gives MyZen's reason")]
    [InlineData("""{"Success":false,"Message":"Project is closed"}""", "Project is closed")]
    [InlineData("""{"Success":false}""", "MyZen refused the change.")]
    [InlineData("""<form><span class="field-validation-error">Date is required</span></form>""", "Date is required")]
    [InlineData("""<div class="validation-summary-errors"><ul><li>Too long</li></ul></div>""", "Too long")]
    public void ParseChangeError_Refused_Reason(string body, string reason)
    {
        MyZenHtml.ParseChangeError(body).Should().Be(reason);
    }
}
