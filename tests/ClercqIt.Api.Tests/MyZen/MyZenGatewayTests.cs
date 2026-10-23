using System.Net;
using System.Text;
using Clercq.It.Application.Features.MyZen;
using Clercq.It.Infrastructure.MyZen;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ClercqIt.Api.Tests.MyZen;

/// <summary>
/// The gateway against a scripted MyZen: each test queues the responses MyZen
/// would give and checks the requests sent and the session stored.
/// </summary>
public class MyZenGatewayTests
{
    private const string BaseUrl = "https://myzen.example";

    private readonly FakeMyZen _myZen = new();
    private readonly InMemorySessionStore _store = new();
    private readonly MyZenOptions _options = new()
    {
        BaseUrl = BaseUrl,
        Email = "owner@example.com",
        Password = "pw",
        SessionKey = "test-session-key",
    };

    private MyZenGateway Gateway() =>
        new(_store, _myZen, new MyZenSessionLock(), Options.Create(_options), TimeProvider.System,
            NullLogger<MyZenGateway>.Instance);

    [Fact(DisplayName = "Without a stored session the status is Disconnected")]
    public async Task GetStatus_NoSession_Disconnected()
    {
        var status = await Gateway().GetStatusAsync(CancellationToken.None);

        status.Should().Be(new MyZenStatusDto(MyZenSessionState.Disconnected, null, null));
    }

    [Fact(DisplayName = "Starting the login stores a Connecting session with the page's cookies and tokens")]
    public async Task StartLogin_StoresConnectingSession()
    {
        // Arrange
        _myZen.Respond(HttpMethod.Get, "/Account/Logon", MyZenPages.Login(captcha: true), setCookie: ("ASP.NET_SessionId", "s1"));

        // Act
        var step = await Gateway().StartLoginAsync(CancellationToken.None);

        // Assert
        step.Should().Be(new MyZenLoginStepDto(MyZenLoginStep.Credentials, MyZenPages.CaptchaImage, null));
        _store.Record!.State.Should().Be(MyZenSessionState.Connecting);
        var data = _store.Data(_options.SessionKey)!;
        data.Cookies.Should().ContainSingle(c => c.Name == "ASP.NET_SessionId" && c.Value == "s1");
        data.FormToken.Should().Be("form-token");
        data.HeaderToken.Should().Be("header-token");
        data.CaptchaCheck.Should().Be("captcha-check");
        (await Gateway().GetStatusAsync(CancellationToken.None)).State.Should().Be(MyZenSessionState.Connecting);
    }

    [Fact(DisplayName = "Without credentials the login is refused as unavailable")]
    public async Task StartLogin_NotConfigured_Throws()
    {
        // Arrange
        _options.Password = "";

        // Act
        var act = () => Gateway().StartLoginAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<MyZenUnavailableException>();
    }

    [Fact(DisplayName = "Submitting before starting asks to start again")]
    public async Task SubmitLogin_NotStarted_AsksToStart()
    {
        var step = await Gateway().SubmitLoginAsync(null, CancellationToken.None);

        step.Message.Should().Be("Start the connection again.");
        _myZen.Requests.Should().BeEmpty();
    }

    [Fact(DisplayName = "A login without MFA posts the form with the CAPTCHA and connects")]
    public async Task SubmitLogin_NoMfa_Connects()
    {
        // Arrange
        await StartAsync();
        _myZen.Respond(HttpMethod.Post, "/Account/ShowTwoFactorAuthentication", """{"ShowMfaAppAuthentication":false}""");
        _myZen.Respond(HttpMethod.Post, "/Account/Logon", "<html>dashboard</html>", finalPath: "/Home/Dashboard",
            setCookie: (".ASPXAUTH", "auth"));

        // Act
        var step = await Gateway().SubmitLoginAsync("ab12", CancellationToken.None);

        // Assert
        step.Step.Should().Be(MyZenLoginStep.Connected);
        var mfaCheck = _myZen.Requests[1];
        mfaCheck.Headers["RequestVerificationToken"].Should().Be("header-token");
        mfaCheck.Form.Should().Contain("email=owner%40example.com").And.Contain("password=pw");
        var form = _myZen.Requests[2].Form;
        form.Should().Contain("Captcha=ab12").And.Contain("CaptchaCheck=captcha-check")
            .And.Contain("RememberMe=true").And.Contain("__RequestVerificationToken=form-token");
        _myZen.Requests[2].Headers.Should().NotContainKey("RequestVerificationToken");

        var status = await Gateway().GetStatusAsync(CancellationToken.None);
        status.State.Should().Be(MyZenSessionState.Connected);
        status.ConnectedAt.Should().NotBeNull();
        status.LastSeenAlive.Should().NotBeNull();
        _store.Data(_options.SessionKey)!.Cookies.Select(c => c.Name).Should().Contain(".ASPXAUTH");
    }

    [Fact(DisplayName = "A refused login shows the new CAPTCHA and MyZen's message")]
    public async Task SubmitLogin_Refused_ShowsCaptcha()
    {
        // Arrange
        await StartAsync();
        _myZen.Respond(HttpMethod.Post, "/Account/ShowTwoFactorAuthentication", "not json");
        _myZen.Respond(HttpMethod.Post, "/Account/Logon", MyZenPages.Login(captcha: true, error: "Captcha is wrong"));

        // Act
        var step = await Gateway().SubmitLoginAsync(null, CancellationToken.None);

        // Assert
        step.Should().Be(new MyZenLoginStepDto(MyZenLoginStep.Credentials, MyZenPages.CaptchaImage, "Captcha is wrong"));
        _myZen.Requests[2].Form.Should().Contain("Captcha=&");
        _store.Record!.State.Should().Be(MyZenSessionState.Connecting);
    }

    [Fact(DisplayName = "A refused login without a MyZen message gives a default one")]
    public async Task SubmitLogin_RefusedWithoutMessage_DefaultMessage()
    {
        // Arrange
        await StartAsync();
        _myZen.Respond(HttpMethod.Post, "/Account/ShowTwoFactorAuthentication", "[]", status: HttpStatusCode.OK);
        _myZen.Respond(HttpMethod.Post, "/Account/Logon", MyZenPages.Login());

        // Act
        var step = await Gateway().SubmitLoginAsync(null, CancellationToken.None);

        // Assert
        step.Message.Should().StartWith("MyZen did not accept the sign-in.");
        step.CaptchaImage.Should().BeNull();
    }

    [Fact(DisplayName = "A login with app MFA asks for the code, and a good code connects")]
    public async Task SubmitLogin_Mfa_ThenCode_Connects()
    {
        // Arrange
        await StartAsync();
        _myZen.Respond(HttpMethod.Post, "/Account/ShowTwoFactorAuthentication", """{"ShowMfaAppAuthentication":true}""");
        _myZen.Respond(HttpMethod.Post, "/Account/Mfa", """{"Success":true,"Result":{"RedirectUrl":"/Home/Dashboard"}}""");
        _myZen.Respond(HttpMethod.Get, "/Home/Dashboard", "<html>dashboard</html>");

        // Act
        var first = await Gateway().SubmitLoginAsync(null, CancellationToken.None);
        var second = await Gateway().SubmitMfaCodeAsync("123456", CancellationToken.None);

        // Assert
        first.Step.Should().Be(MyZenLoginStep.Mfa);
        second.Step.Should().Be(MyZenLoginStep.Connected);
        var mfa = _myZen.Requests.Single(r => r.Path == "/Account/Mfa");
        mfa.Form.Should().Contain("mfaCode=123456").And.Contain("RememberThisDevice=true");
        mfa.Headers["RequestVerificationToken"].Should().Be("header-token");
        _store.Record!.State.Should().Be(MyZenSessionState.Connected);
    }

    [Theory(DisplayName = "MyZen's redirect after the code is followed only when it is a local path")]
    [InlineData("/TimeRegistration/Index", "/TimeRegistration/Index")]
    [InlineData("https://evil.example/", "/Home/Dashboard")]
    [InlineData("//evil.example/", "/Home/Dashboard")]
    [InlineData(null, "/Home/Dashboard")]
    public async Task SubmitMfa_Redirect_OnlyLocal(string? redirect, string expectedPath)
    {
        // Arrange
        await StartAsync();
        await MfaPendingAsync();
        var result = redirect is null ? "{}" : $$"""{"RedirectUrl":"{{redirect}}"}""";
        _myZen.Respond(HttpMethod.Post, "/Account/Mfa", $$"""{"Success":true,"Result":{{result}}}""");
        _myZen.Respond(HttpMethod.Get, expectedPath, "<html>ok</html>");

        // Act
        var step = await Gateway().SubmitMfaCodeAsync("123456", CancellationToken.None);

        // Assert
        step.Step.Should().Be(MyZenLoginStep.Connected);
        _myZen.Requests[^1].Path.Should().Be(expectedPath);
    }

    [Theory(DisplayName = "A refused MFA code asks for the next one")]
    [InlineData(HttpStatusCode.BadRequest, "")]
    [InlineData(HttpStatusCode.OK, """{"Success":false}""")]
    [InlineData(HttpStatusCode.OK, "oops")]
    [InlineData(HttpStatusCode.OK, "[]")]
    public async Task SubmitMfa_Refused_AsksAgain(HttpStatusCode status, string body)
    {
        // Arrange
        await StartAsync();
        await MfaPendingAsync();
        _myZen.Respond(HttpMethod.Post, "/Account/Mfa", body, status: status);

        // Act
        var step = await Gateway().SubmitMfaCodeAsync("123456", CancellationToken.None);

        // Assert
        step.Should().Be(new MyZenLoginStepDto(MyZenLoginStep.Mfa, null, "MyZen did not accept the code. Try the next one."));
        _store.Record!.State.Should().Be(MyZenSessionState.Connecting);
    }

    [Fact(DisplayName = "When MyZen asks to sign in again after the code, the login starts over")]
    public async Task SubmitMfa_BackOnLogin_StartsOver()
    {
        // Arrange
        await StartAsync();
        await MfaPendingAsync();
        _myZen.Respond(HttpMethod.Post, "/Account/Mfa", """{"Success":true,"Result":{"RedirectUrl":"/Home/Dashboard"}}""");
        _myZen.Respond(HttpMethod.Get, "/Home/Dashboard", MyZenPages.Login(captcha: true), finalPath: "/Account/Logon");

        // Act
        var step = await Gateway().SubmitMfaCodeAsync("123456", CancellationToken.None);

        // Assert
        step.Step.Should().Be(MyZenLoginStep.Credentials);
        step.CaptchaImage.Should().Be(MyZenPages.CaptchaImage);
    }

    [Fact(DisplayName = "Sending a code before the login asks to start again")]
    public async Task SubmitMfa_NotStarted_AsksToStart()
    {
        var step = await Gateway().SubmitMfaCodeAsync("123456", CancellationToken.None);

        step.Message.Should().Be("Start the connection again.");
    }

    [Fact(DisplayName = "Disconnecting removes the stored session")]
    public async Task Disconnect_RemovesSession()
    {
        // Arrange
        await ConnectAsync();

        // Act
        await Gateway().DisconnectAsync(CancellationToken.None);

        // Assert
        _store.Record.Should().BeNull();
    }

    [Fact(DisplayName = "The keep-alive touches a live session and refreshes its last-seen time")]
    public async Task KeepAlive_Live_ReturnsTrue()
    {
        // Arrange
        await ConnectAsync();
        _store.Record!.LastSeenAlive = DateTimeOffset.UnixEpoch;
        _myZen.Respond(HttpMethod.Get, "/TimeRegistration/Index", MyZenPages.TimeRegistrationIndex);

        // Act
        var alive = await Gateway().KeepAliveAsync(CancellationToken.None);

        // Assert
        alive.Should().BeTrue();
        _store.Record!.LastSeenAlive.Should().BeAfter(DateTimeOffset.UnixEpoch);
        _store.Record!.ConnectedAt.Should().NotBeNull();
    }

    [Fact(DisplayName = "The keep-alive marks the session expired when MyZen shows its login page")]
    public async Task KeepAlive_Expired_MarksExpired()
    {
        // Arrange
        await ConnectAsync();
        _myZen.Respond(HttpMethod.Get, "/TimeRegistration/Index", MyZenPages.Login(), finalPath: "/Account/Logon");

        // Act
        var alive = await Gateway().KeepAliveAsync(CancellationToken.None);

        // Assert
        alive.Should().BeFalse();
        _store.Record!.State.Should().Be(MyZenSessionState.Expired);
        _store.Record!.Payload.Should().BeNull();
    }

    [Fact(DisplayName = "The keep-alive does nothing without a connected session")]
    public async Task KeepAlive_NoSession_ReturnsFalse()
    {
        (await Gateway().KeepAliveAsync(CancellationToken.None)).Should().BeFalse();
        _myZen.Requests.Should().BeEmpty();
    }

    [Fact(DisplayName = "Data calls without a session throw NotConnected")]
    public async Task Data_NoSession_ThrowsNotConnected()
    {
        var act = () => Gateway().GetProjectsAsync(CancellationToken.None);

        await act.Should().ThrowAsync<MyZenNotConnectedException>();
    }

    [Fact(DisplayName = "A session encrypted with another key counts as not connected")]
    public async Task Data_OtherKey_ThrowsNotConnected()
    {
        // Arrange
        await ConnectAsync();
        _options.SessionKey = "rotated-key";

        // Act
        var act = () => Gateway().GetProjectsAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<MyZenNotConnectedException>();
    }

    [Fact(DisplayName = "Projects are read page by page")]
    public async Task GetProjects_ReadsAllPages()
    {
        // Arrange
        await ConnectAsync();
        _myZen.Respond(HttpMethod.Get, "/Project/Index", MyZenPages.ProjectList(0, 1));
        _myZen.Respond(HttpMethod.Get, "/Project/Index", MyZenPages.ProjectList(0, 1));

        // Act
        var projects = await Gateway().GetProjectsAsync(CancellationToken.None);

        // Assert
        projects.Should().HaveCount(2);
        _myZen.Requests.Select(r => r.Query).Should().Equal("?F.CurrentPage=0", "?F.CurrentPage=1");
    }

    [Fact(DisplayName = "An expired session during a data call is marked expired and throws NotConnected")]
    public async Task GetProjects_Expired_Throws()
    {
        // Arrange
        await ConnectAsync();
        _myZen.Respond(HttpMethod.Get, "/Project/Index", MyZenPages.Login(), finalPath: "/Account/Logon");

        // Act
        var act = () => Gateway().GetProjectsAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<MyZenNotConnectedException>();
        _store.Record!.State.Should().Be(MyZenSessionState.Expired);
    }

    [Fact(DisplayName = "Time entries use the page token and filter, the period and every page")]
    public async Task GetTimeEntries_ReadsAllPages()
    {
        // Arrange
        await ConnectAsync();
        _myZen.Respond(HttpMethod.Get, "/TimeRegistration/Index", MyZenPages.TimeRegistrationIndex);
        _myZen.Respond(HttpMethod.Post, "/TimeRegistration/TimeRegistrationEntriesList", MyZenPages.EntryList(
            [MyZenPages.EntryRow(1, "01/09/2026", "a", "c", "p", 3600, false)], 0, 1));
        _myZen.Respond(HttpMethod.Post, "/TimeRegistration/TimeRegistrationEntriesList", MyZenPages.EntryList(
            [MyZenPages.EntryRow(2, "02/09/2026", "b", "c", "p", 3600, false)], 0, 1));

        // Act
        var entries = await Gateway().GetTimeEntriesAsync(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30), CancellationToken.None);

        // Assert
        entries.Select(e => e.Id).Should().Equal(1, 2);
        var first = _myZen.Requests[1];
        first.Headers["RequestVerificationToken"].Should().Be("page-token");
        first.Form.Should().Contain("F.UserID=4242").And.Contain("F.DateFrom=01%2F09%2F2026")
            .And.Contain("F.DateTill=30%2F09%2F2026").And.Contain("F.CurrentPage=0")
            .And.NotContain("F.DateFrom=01%2F10%2F2026");
        _myZen.Requests[2].Form.Should().Contain("F.CurrentPage=1");
    }

    [Fact(DisplayName = "Time entries throw NotConnected when the index or list shows the login page")]
    public async Task GetTimeEntries_Expired_Throws()
    {
        // Arrange
        await ConnectAsync();
        _myZen.Respond(HttpMethod.Get, "/TimeRegistration/Index", MyZenPages.TimeRegistrationIndex);
        _myZen.Respond(HttpMethod.Post, "/TimeRegistration/TimeRegistrationEntriesList", MyZenPages.Login(), finalPath: "/Account/Logon");

        // Act
        var act = () => Gateway().GetTimeEntriesAsync(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<MyZenNotConnectedException>();
    }

    [Fact(DisplayName = "Time entries throw NotConnected when the index shows the login page")]
    public async Task GetTimeEntries_IndexExpired_Throws()
    {
        // Arrange
        await ConnectAsync();
        _myZen.Respond(HttpMethod.Get, "/TimeRegistration/Index", MyZenPages.Login(), finalPath: "/Account/Logon");

        // Act
        var act = () => Gateway().GetTimeEntriesAsync(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<MyZenNotConnectedException>();
    }

    [Fact(DisplayName = "Time entries need the page's request token")]
    public async Task GetTimeEntries_NoToken_Unavailable()
    {
        // Arrange
        await ConnectAsync();
        _myZen.Respond(HttpMethod.Get, "/TimeRegistration/Index", "<html></html>");

        // Act
        var act = () => Gateway().GetTimeEntriesAsync(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<MyZenUnavailableException>();
    }

    [Fact(DisplayName = "One time entry is read from its edit form")]
    public async Task GetTimeEntry_ReadsDetails()
    {
        // Arrange
        await ConnectAsync();
        _myZen.Respond(HttpMethod.Get, "/TimeRegistration/TimeRegistrationEntryDetails", MyZenPages.EntryDetails(101));

        // Act
        var entry = await Gateway().GetTimeEntryAsync(101, CancellationToken.None);

        // Assert
        entry!.ProjectId.Should().Be(555);
        _myZen.Requests[0].Query.Should().Be("?ID=101&projectSelectable=true");
    }

    [Fact(DisplayName = "An unknown time entry gives nothing")]
    public async Task GetTimeEntry_Unknown_ReturnsNull()
    {
        // Arrange
        await ConnectAsync();
        _myZen.Respond(HttpMethod.Get, "/TimeRegistration/TimeRegistrationEntryDetails", "<form></form>");

        // Act & Assert
        (await Gateway().GetTimeEntryAsync(5, CancellationToken.None)).Should().BeNull();
    }

    [Fact(DisplayName = "A time entry read throws NotConnected when MyZen shows its login page")]
    public async Task GetTimeEntry_Expired_Throws()
    {
        // Arrange
        await ConnectAsync();
        _myZen.Respond(HttpMethod.Get, "/TimeRegistration/TimeRegistrationEntryDetails", MyZenPages.Login(), finalPath: "/Account/Logon");

        // Act
        var act = () => Gateway().GetTimeEntryAsync(5, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<MyZenNotConnectedException>();
    }

    [Fact(DisplayName = "A network failure becomes MyZen unavailable")]
    public async Task NetworkFailure_Unavailable()
    {
        // Arrange
        _myZen.Fail = true;

        // Act
        var act = () => Gateway().StartLoginAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<MyZenUnavailableException>().WithMessage("MyZen could not be reached.");
    }

    [Fact(DisplayName = "The real handler factory keeps cookies and follows redirects")]
    public void HandlerFactory_UsesCookiesAndRedirects()
    {
        // Arrange
        var cookies = new CookieContainer();

        // Act
        using var handler = (SocketsHttpHandler)new MyZenHttpHandlerFactory().Create(cookies);

        // Assert
        handler.CookieContainer.Should().BeSameAs(cookies);
        handler.AllowAutoRedirect.Should().BeTrue();
    }

    private async Task StartAsync()
    {
        _myZen.Respond(HttpMethod.Get, "/Account/Logon", MyZenPages.Login(), setCookie: ("ASP.NET_SessionId", "s1"));
        await Gateway().StartLoginAsync(CancellationToken.None);
    }

    private async Task MfaPendingAsync()
    {
        _myZen.Respond(HttpMethod.Post, "/Account/ShowTwoFactorAuthentication", """{"ShowMfaAppAuthentication":true}""");
        await Gateway().SubmitLoginAsync(null, CancellationToken.None);
    }

    private async Task ConnectAsync()
    {
        await StartAsync();
        _myZen.Respond(HttpMethod.Post, "/Account/ShowTwoFactorAuthentication", """{"ShowMfaAppAuthentication":false}""");
        _myZen.Respond(HttpMethod.Post, "/Account/Logon", "<html>dashboard</html>", finalPath: "/Home/Dashboard");
        await Gateway().SubmitLoginAsync(null, CancellationToken.None);
        _myZen.Requests.Clear();
    }

    private sealed class InMemorySessionStore : IMyZenSessionStore
    {
        public MyZenSessionRecord? Record { get; private set; }

        public MyZenSessionData? Data(string key) => new MyZenSessionCipher(key).Decrypt(Record!.Payload!);

        public Task<MyZenSessionRecord?> LoadAsync(CancellationToken cancellationToken) => Task.FromResult(Record);

        public Task SaveAsync(MyZenSessionRecord record, CancellationToken cancellationToken)
        {
            Record = record;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(CancellationToken cancellationToken)
        {
            Record = null;
            return Task.CompletedTask;
        }
    }

    internal sealed record SentRequest(string Method, string Path, string Query, Dictionary<string, string> Headers, string Form);

    /// <summary>A scripted MyZen: answers queued responses in order and records what was sent.</summary>
    private sealed class FakeMyZen : IMyZenHttpHandlerFactory
    {
        private readonly Queue<(HttpMethod Method, string Path, Func<CookieContainer, HttpRequestMessage, HttpResponseMessage> Reply)> _replies = new();

        public List<SentRequest> Requests { get; } = [];
        public bool Fail { get; set; }

        public void Respond(HttpMethod method, string path, string body, HttpStatusCode status = HttpStatusCode.OK,
            string? finalPath = null, (string Name, string Value)? setCookie = null) =>
            _replies.Enqueue((method, path, (cookies, request) =>
            {
                if (setCookie is { } c)
                {
                    cookies.Add(new Cookie(c.Name, c.Value, "/", "myzen.example") { HttpOnly = true, Secure = true });
                }
                return new HttpResponseMessage(status)
                {
                    Content = new StringContent(body, Encoding.UTF8),
                    RequestMessage = finalPath is null ? request : new HttpRequestMessage(HttpMethod.Get, BaseUrl + finalPath),
                };
            }
        ));

        public HttpMessageHandler Create(CookieContainer cookies) => new Handler(this, cookies);

        private sealed class Handler(FakeMyZen owner, CookieContainer cookies) : HttpMessageHandler
        {
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                if (owner.Fail)
                {
                    throw new HttpRequestException("down");
                }

                var uri = request.RequestUri!;
                owner.Requests.Add(new SentRequest(
                    request.Method.Method,
                    uri.AbsolutePath,
                    uri.Query,
                    request.Headers.ToDictionary(h => h.Key, h => string.Join(",", h.Value)),
                    request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken)));

                var (method, path, reply) = owner._replies.Dequeue();
                method.Should().Be(request.Method);
                path.Should().Be(uri.AbsolutePath);
                return reply(cookies, request);
            }
        }
    }
}
