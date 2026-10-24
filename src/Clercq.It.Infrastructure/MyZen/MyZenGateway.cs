using System.Globalization;
using System.Net;
using System.Text.Json;
using Clercq.It.Application.Features.MyZen;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Clercq.It.Infrastructure.MyZen;

/// <summary>Creates the HTTP handler for one MyZen exchange, around that session's cookies.</summary>
public interface IMyZenHttpHandlerFactory
{
    HttpMessageHandler Create(CookieContainer cookies);
}

public sealed class MyZenHttpHandlerFactory : IMyZenHttpHandlerFactory
{
    public HttpMessageHandler Create(CookieContainer cookies) => new SocketsHttpHandler
    {
        CookieContainer = cookies,
        UseCookies = true,
        AllowAutoRedirect = true,
        MaxAutomaticRedirections = 10,
    };
}

/// <summary>Serialises MyZen exchanges: the keep-alive and a request must not both rewrite the cookies.</summary>
public sealed class MyZenSessionLock
{
    public SemaphoreSlim Gate { get; } = new(1, 1);
}

/// <summary>
/// Talks to MyZen like its own web pages do: a cookie session, the login form,
/// the MFA call and the AJAX endpoints mapped in issue #345. Credentials come
/// from configuration; the session (cookies, tokens) is stored encrypted.
/// Never logs cookies, tokens, credentials or MyZen's data.
/// </summary>
public sealed class MyZenGateway(
    IMyZenSessionStore store,
    IMyZenHttpHandlerFactory handlers,
    MyZenSessionLock sessionLock,
    IOptions<MyZenOptions> options,
    TimeProvider clock,
    ILogger<MyZenGateway> logger) : IMyZenGateway
{
    private const string LogonPath = "/Account/Logon";
    private const string KeepAlivePath = "/TimeRegistration/Index";
    private const int MaxPages = 50;

    private readonly MyZenOptions _options = options.Value;
    private readonly MyZenSessionCipher _cipher = new(options.Value.SessionKey);

    public async Task<MyZenStatusDto> GetStatusAsync(CancellationToken cancellationToken)
    {
        var record = await store.LoadAsync(cancellationToken);
        return record is null
            ? new MyZenStatusDto(MyZenSessionState.Disconnected, null, null)
            : new MyZenStatusDto(record.State, record.ConnectedAt, record.LastSeenAlive);
    }

    public Task<MyZenLoginStepDto> StartLoginAsync(CancellationToken cancellationToken) =>
        Locked(async () =>
        {
            EnsureConfigured();
            var session = new Session(_options.BaseUrl, handlers, data: null);
            var response = await session.GetAsync(LogonPath, cancellationToken);
            var page = MyZenHtml.ParseLoginPage(response.Body);
            await SaveAsync(session, MyZenSessionState.Connecting, page, connectedAt: null, cancellationToken);
            logger.LogInformation("MyZen login started (CAPTCHA shown: {CaptchaShown})", page.CaptchaImage is not null);
            return new MyZenLoginStepDto(MyZenLoginStep.Credentials, page.CaptchaImage, null);
        }, cancellationToken);

    public Task<MyZenLoginStepDto> SubmitLoginAsync(string? captcha, CancellationToken cancellationToken) =>
        Locked(async () =>
        {
            EnsureConfigured();
            var (record, data) = await LoadAsync(cancellationToken);
            if (record?.State != MyZenSessionState.Connecting || data is null)
            {
                return new MyZenLoginStepDto(MyZenLoginStep.Credentials, null, "Start the connection again.");
            }

            var session = new Session(_options.BaseUrl, handlers, data);

            var mfaCheck = await session.PostAsync("/Account/ShowTwoFactorAuthentication", Form(
                ("email", _options.Email),
                ("password", _options.Password)), data.HeaderToken, cancellationToken);
            if (mfaCheck.Status == HttpStatusCode.OK && AsksForAppCode(mfaCheck.Body))
            {
                await SaveAsync(session, MyZenSessionState.Connecting, page: null, connectedAt: null, cancellationToken);
                logger.LogInformation("MyZen login needs an MFA code");
                return new MyZenLoginStepDto(MyZenLoginStep.Mfa, null, null);
            }

            var login = await session.PostAsync(LogonPath, Form(
                ("Email", _options.Email),
                ("Password", _options.Password),
                ("Captcha", captcha ?? string.Empty),
                ("CaptchaCheck", data.CaptchaCheck ?? string.Empty),
                ("RememberMe", "true"),
                ("__RequestVerificationToken", data.FormToken ?? string.Empty)), headerToken: null, cancellationToken);

            if (!login.IsLoginPage)
            {
                return await ConnectedAsync(session, cancellationToken);
            }

            var page = MyZenHtml.ParseLoginPage(login.Body);
            await SaveAsync(session, MyZenSessionState.Connecting, page, connectedAt: null, cancellationToken);
            logger.LogWarning("MyZen did not accept the login (CAPTCHA shown: {CaptchaShown})", page.CaptchaImage is not null);
            return new MyZenLoginStepDto(MyZenLoginStep.Credentials, page.CaptchaImage,
                page.Error ?? "MyZen did not accept the sign-in. Type the CAPTCHA if one is shown and try again.");
        }, cancellationToken);

    public Task<MyZenLoginStepDto> SubmitMfaCodeAsync(string code, CancellationToken cancellationToken) =>
        Locked(async () =>
        {
            EnsureConfigured();
            var (record, data) = await LoadAsync(cancellationToken);
            if (record?.State != MyZenSessionState.Connecting || data is null)
            {
                return new MyZenLoginStepDto(MyZenLoginStep.Credentials, null, "Start the connection again.");
            }

            var session = new Session(_options.BaseUrl, handlers, data);
            var response = await session.PostAsync("/Account/Mfa", Form(
                ("email", _options.Email),
                ("password", _options.Password),
                ("mfaCode", code),
                ("RememberThisDevice", "true"),
                ("client_id", string.Empty),
                ("redirect_uri", string.Empty),
                ("state", string.Empty),
                ("ReturnUrl", string.Empty)), data.HeaderToken, cancellationToken);

            var redirect = response.Status == HttpStatusCode.OK ? MfaRedirect(response.Body) : null;
            if (redirect is null)
            {
                await SaveAsync(session, MyZenSessionState.Connecting, page: null, connectedAt: null, cancellationToken);
                logger.LogWarning("MyZen rejected the MFA code");
                return new MyZenLoginStepDto(MyZenLoginStep.Mfa, null, "MyZen did not accept the code. Try the next one.");
            }

            var landing = await session.GetAsync(redirect, cancellationToken);
            if (landing.IsLoginPage)
            {
                var page = MyZenHtml.ParseLoginPage(landing.Body);
                await SaveAsync(session, MyZenSessionState.Connecting, page, connectedAt: null, cancellationToken);
                return new MyZenLoginStepDto(MyZenLoginStep.Credentials, page.CaptchaImage,
                    "MyZen asked to sign in again after the code. Try once more.");
            }

            return await ConnectedAsync(session, cancellationToken);
        }, cancellationToken);

    public async Task DisconnectAsync(CancellationToken cancellationToken)
    {
        await sessionLock.Gate.WaitAsync(cancellationToken);
        try
        {
            await store.DeleteAsync(cancellationToken);
            logger.LogInformation("MyZen session removed");
        }
        finally
        {
            sessionLock.Gate.Release();
        }
    }

    public Task<bool> KeepAliveAsync(CancellationToken cancellationToken) =>
        Locked(async () =>
        {
            var (record, data) = await LoadAsync(cancellationToken);
            if (record?.State != MyZenSessionState.Connected || data is null)
            {
                return false;
            }

            var session = new Session(_options.BaseUrl, handlers, data);
            var response = await session.GetAsync(KeepAlivePath, cancellationToken);
            if (response.IsLoginPage)
            {
                await ExpireAsync(record, cancellationToken);
                return false;
            }

            await SaveAsync(session, MyZenSessionState.Connected, page: null, record.ConnectedAt, cancellationToken);
            return true;
        }, cancellationToken);

    public Task<IReadOnlyList<MyZenProjectDto>> GetProjectsAsync(CancellationToken cancellationToken) =>
        WithSession<IReadOnlyList<MyZenProjectDto>>(async session =>
        {
            var projects = new List<MyZenProjectDto>();
            for (var page = 0; page < MaxPages; page++)
            {
                var response = await session.GetAsync($"/Project/Index?F.CurrentPage={page}", cancellationToken);
                if (response.IsLoginPage)
                {
                    return null;
                }

                var (items, hasNext) = MyZenHtml.ParseProjectList(response.Body, page);
                projects.AddRange(items);
                if (!hasNext)
                {
                    break;
                }
            }
            return projects;
        }, cancellationToken);

    public Task<IReadOnlyList<MyZenTimeEntryDto>> GetTimeEntriesAsync(DateOnly firstDay, DateOnly lastDay, CancellationToken cancellationToken) =>
        WithSession<IReadOnlyList<MyZenTimeEntryDto>>(async session =>
        {
            var index = await session.GetAsync(KeepAlivePath, cancellationToken);
            if (index.IsLoginPage)
            {
                return null;
            }

            var token = MyZenHtml.ParseRequestVerificationToken(index.Body)
                ?? throw new MyZenUnavailableException("MyZen's time registration page has no request token.");
            var filter = MyZenHtml.ParseTimeEntryFilter(index.Body)
                .Where(f => f.Key is not ("F.DateFrom" or "F.DateTill" or "F.CurrentPage"))
                .ToList();

            var entries = new List<MyZenTimeEntryDto>();
            for (var page = 0; page < MaxPages; page++)
            {
                var form = filter.Concat(Form(
                    ("F.DateFrom", firstDay.ToString(MyZenHtml.DateFormat, CultureInfo.InvariantCulture)),
                    ("F.DateTill", lastDay.ToString(MyZenHtml.DateFormat, CultureInfo.InvariantCulture)),
                    ("F.CurrentPage", page.ToString(CultureInfo.InvariantCulture))));
                var response = await session.PostAsync("/TimeRegistration/TimeRegistrationEntriesList", form, token, cancellationToken);
                if (response.IsLoginPage)
                {
                    return null;
                }

                var list = MyZenHtml.ParseTimeEntryList(response.Body, page);
                entries.AddRange(list.Entries);
                if (!list.HasNextPage)
                {
                    break;
                }
            }
            return entries;
        }, cancellationToken);

    public async Task<MyZenTimeEntryDto?> GetTimeEntryAsync(long id, CancellationToken cancellationToken)
    {
        var found = await WithSession<IReadOnlyList<MyZenTimeEntryDto>>(async session =>
        {
            var response = await session.GetAsync(
                $"/TimeRegistration/TimeRegistrationEntryDetails?ID={id.ToString(CultureInfo.InvariantCulture)}&projectSelectable=true",
                cancellationToken);
            if (response.IsLoginPage)
            {
                return null;
            }

            return MyZenHtml.ParseTimeEntryDetails(response.Body) is { } entry && entry.Id == id ? [entry] : [];
        }, cancellationToken);
        return found.SingleOrDefault();
    }

    public Task<IReadOnlyList<MyZenProjectOptionDto>> SearchProjectsAsync(string query, CancellationToken cancellationToken) =>
        WithSession<IReadOnlyList<MyZenProjectOptionDto>>(async session =>
        {
            if (await RequestTokenAsync(session, cancellationToken) is not { } token)
            {
                return null;
            }

            var response = await session.PostAsync("/Base/GetAutoSuggestValues", Form(
                ("q", query),
                ("option1", string.Empty),
                ("option2", string.Empty),
                ("option3", string.Empty),
                ("option4", string.Empty),
                ("autoSuggestType", "Projects"),
                ("delay", "0")), token, cancellationToken);
            return response.IsLoginPage ? null : MyZenHtml.ParseProjectSuggestions(response.Body);
        }, cancellationToken);

    public Task CreateTimeEntryAsync(MyZenTimeEntryInput entry, CancellationToken cancellationToken) =>
        WithSession(async session =>
        {
            if (await RequestTokenAsync(session, cancellationToken) is not { } token)
            {
                return null;
            }

            return await SaveEntryAsync(session, token, id: null, entry, cancellationToken);
        }, cancellationToken);

    public async Task<bool> UpdateTimeEntryAsync(long id, MyZenTimeEntryInput entry, CancellationToken cancellationToken)
    {
        var outcome = await WithSession(async session =>
        {
            if (await EntryExistsAsync(session, id, cancellationToken) is not { } exists)
            {
                return null;
            }
            if (!exists)
            {
                return Outcome.NotFound;
            }
            if (await RequestTokenAsync(session, cancellationToken) is not { } token)
            {
                return null;
            }

            return await SaveEntryAsync(session, token, id, entry, cancellationToken);
        }, cancellationToken);
        return outcome.Found;
    }

    public async Task<bool> DeleteTimeEntryAsync(long id, CancellationToken cancellationToken)
    {
        var outcome = await WithSession(async session =>
        {
            if (await EntryExistsAsync(session, id, cancellationToken) is not { } exists)
            {
                return null;
            }
            if (!exists)
            {
                return Outcome.NotFound;
            }
            if (await RequestTokenAsync(session, cancellationToken) is not { } token)
            {
                return null;
            }

            var response = await session.PostAsync("/TimeRegistration/DeleteTimeRegistration",
                Form(("ID", id.ToString(CultureInfo.InvariantCulture))), token, cancellationToken, newUi: true);
            if (response.IsLoginPage)
            {
                return null;
            }

            EnsureAccepted(response, "delete");
            logger.LogInformation("MyZen time entry deleted");
            return Outcome.Done;
        }, cancellationToken);
        return outcome.Found;
    }

    private async Task<Outcome?> SaveEntryAsync(Session session, string token, long? id, MyZenTimeEntryInput entry,
        CancellationToken cancellationToken)
    {
        var response = await session.PostAsync("/TimeRegistration/TimeRegistrationEntryDetails", Form(
            ("TimeRegistrationID", id?.ToString(CultureInfo.InvariantCulture) ?? string.Empty),
            ("ProjectID", entry.ProjectId.ToString(CultureInfo.InvariantCulture)),
            ("CustomerID", entry.CustomerId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty),
            ("Date", entry.Date.ToString(MyZenHtml.DateFormat, CultureInfo.InvariantCulture)),
            ("Duration", (entry.DurationMinutes * 60).ToString(CultureInfo.InvariantCulture)),
            ("Description", entry.Description ?? string.Empty),
            ("Invoiced", entry.Invoiced ? "true" : "false")), token, cancellationToken, newUi: true);
        if (response.IsLoginPage)
        {
            return null;
        }

        EnsureAccepted(response, "save");
        logger.LogInformation("MyZen time entry {Action}", id is null ? "created" : "updated");
        return Outcome.Done;
    }

    private void EnsureAccepted(Reply response, string action)
    {
        if ((int)response.Status is < 200 or > 299)
        {
            logger.LogWarning("MyZen answered {Status} to a {Action}", (int)response.Status, action);
            throw new MyZenUnavailableException($"MyZen answered {(int)response.Status} to the {action}.");
        }

        if (MyZenHtml.ParseChangeError(response.Body) is { } error)
        {
            logger.LogWarning("MyZen refused a {Action}", action);
            throw new MyZenRejectedException(error);
        }
    }

    /// <summary>Whether MyZen has an entry with this ID; null when MyZen showed its login page.</summary>
    private static async Task<bool?> EntryExistsAsync(Session session, long id, CancellationToken cancellationToken)
    {
        var response = await session.GetAsync(
            $"/TimeRegistration/TimeRegistrationEntryDetails?ID={id.ToString(CultureInfo.InvariantCulture)}&projectSelectable=true",
            cancellationToken);
        if (response.IsLoginPage)
        {
            return null;
        }

        return MyZenHtml.ParseTimeEntryDetails(response.Body)?.Id == id;
    }

    /// <summary>The anti-forgery token for MyZen's AJAX calls; null when MyZen showed its login page.</summary>
    private static async Task<string?> RequestTokenAsync(Session session, CancellationToken cancellationToken)
    {
        var index = await session.GetAsync(KeepAlivePath, cancellationToken);
        if (index.IsLoginPage)
        {
            return null;
        }

        return MyZenHtml.ParseRequestVerificationToken(index.Body)
            ?? throw new MyZenUnavailableException("MyZen's time registration page has no request token.");
    }

    /// <summary>
    /// Runs a data call on the connected session. The call returns null when MyZen
    /// answered with its login page: the session is then marked expired.
    /// </summary>
    private Task<T> WithSession<T>(Func<Session, Task<T?>> call, CancellationToken cancellationToken) where T : class =>
        Locked(async () =>
        {
            var (record, data) = await LoadAsync(cancellationToken);
            if (record?.State != MyZenSessionState.Connected || data is null)
            {
                throw new MyZenNotConnectedException();
            }

            var session = new Session(_options.BaseUrl, handlers, data);
            T? result;
            try
            {
                result = await call(session);
            }
            catch (MyZenRejectedException)
            {
                // MyZen answered while signed in: keep the cookies it refreshed.
                await SaveAsync(session, MyZenSessionState.Connected, page: null, record.ConnectedAt, cancellationToken);
                throw;
            }

            if (result is null)
            {
                await ExpireAsync(record, cancellationToken);
                throw new MyZenNotConnectedException();
            }

            await SaveAsync(session, MyZenSessionState.Connected, page: null, record.ConnectedAt, cancellationToken);
            return result;
        }, cancellationToken);

    private async Task<MyZenLoginStepDto> ConnectedAsync(Session session, CancellationToken cancellationToken)
    {
        await SaveAsync(session, MyZenSessionState.Connected, page: null, clock.GetUtcNow(), cancellationToken);
        logger.LogInformation("MyZen session connected");
        return new MyZenLoginStepDto(MyZenLoginStep.Connected, null, null);
    }

    private async Task ExpireAsync(MyZenSessionRecord record, CancellationToken cancellationToken)
    {
        record.State = MyZenSessionState.Expired;
        record.Payload = null;
        record.UpdatedAt = clock.GetUtcNow();
        await store.SaveAsync(record, cancellationToken);
        logger.LogWarning("MyZen session expired; the owner must reconnect");
    }

    private async Task<(MyZenSessionRecord? Record, MyZenSessionData? Data)> LoadAsync(CancellationToken cancellationToken)
    {
        var record = await store.LoadAsync(cancellationToken);
        var data = record?.Payload is { } payload ? _cipher.Decrypt(payload) : null;
        return (record, data);
    }

    /// <summary>Stores the session; a login <c>page</c> just read replaces the stored tokens.</summary>
    private async Task SaveAsync(Session session, string state, MyZenLoginPage? page, DateTimeOffset? connectedAt,
        CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var data = session.Snapshot(page);
        await store.SaveAsync(new MyZenSessionRecord
        {
            State = state,
            Payload = _cipher.Encrypt(data),
            ConnectedAt = connectedAt,
            LastSeenAlive = state == MyZenSessionState.Connected ? now : null,
            UpdatedAt = now,
        }, cancellationToken);
    }

    private async Task<T> Locked<T>(Func<Task<T>> action, CancellationToken cancellationToken)
    {
        await sessionLock.Gate.WaitAsync(cancellationToken);
        try
        {
            return await action();
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "MyZen could not be reached");
            throw new MyZenUnavailableException("MyZen could not be reached.");
        }
        finally
        {
            sessionLock.Gate.Release();
        }
    }

    private void EnsureConfigured()
    {
        if (!_options.IsConfigured)
        {
            throw new MyZenUnavailableException("MyZen is not configured on this server.");
        }
    }

    private static List<KeyValuePair<string, string>> Form(params (string Key, string Value)[] fields) =>
        fields.Select(f => KeyValuePair.Create(f.Key, f.Value)).ToList();

    /// <summary>MyZen's login script asks for a code only when the authenticator app is set up.</summary>
    private static bool AsksForAppCode(string body)
    {
        try
        {
            using var json = JsonDocument.Parse(body);
            return json.RootElement.ValueKind == JsonValueKind.Object
                && json.RootElement.TryGetProperty("ShowMfaAppAuthentication", out var app)
                && app.ValueKind == JsonValueKind.True;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>Reads MyZen's answer to an MFA code.</summary>
    /// <returns>Where MyZen sends the browser after a good code, or null when the code was refused.</returns>
    private static string? MfaRedirect(string body)
    {
        try
        {
            using var json = JsonDocument.Parse(body);
            var root = json.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("Success", out var success) || success.ValueKind != JsonValueKind.True)
            {
                return null;
            }

            var url = root.TryGetProperty("Result", out var result) && result.ValueKind == JsonValueKind.Object
                && result.TryGetProperty("RedirectUrl", out var redirect) ? redirect.GetString() : null;
            // Only follow MyZen-relative paths; anything else lands on the dashboard.
            return url is { Length: > 1 } && url[0] == '/' && url[1] != '/' ? url : "/Home/Dashboard";
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>One exchange with MyZen around a cookie jar restored from the stored session.</summary>
    private sealed class Session
    {
        private readonly Uri _baseUri;
        private readonly CookieContainer _cookies = new();
        private readonly IMyZenHttpHandlerFactory _handlers;
        private readonly MyZenSessionData? _data;

        public Session(string baseUrl, IMyZenHttpHandlerFactory handlers, MyZenSessionData? data)
        {
            _baseUri = new Uri(baseUrl);
            _handlers = handlers;
            _data = data;
            foreach (var c in data?.Cookies ?? [])
            {
                _cookies.Add(new Cookie(c.Name, c.Value, c.Path, c.Domain)
                {
                    Expires = c.Expires ?? DateTime.MinValue,
                    Secure = c.Secure,
                    HttpOnly = c.HttpOnly,
                });
            }
        }

        public Task<Reply> GetAsync(string path, CancellationToken cancellationToken) =>
            SendAsync(new HttpRequestMessage(HttpMethod.Get, path), cancellationToken);

        /// <summary>Posts a form; <c>newUi</c> adds the header MyZen's current pages send with their save and delete calls.</summary>
        public Task<Reply> PostAsync(string path, IEnumerable<KeyValuePair<string, string>> form, string? headerToken,
            CancellationToken cancellationToken, bool newUi = false)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = new FormUrlEncodedContent(form) };
            if (headerToken is not null)
            {
                request.Headers.Add("RequestVerificationToken", headerToken);
                request.Headers.Add("X-Requested-With", "XMLHttpRequest");
            }
            if (newUi)
            {
                request.Headers.Add("newui", "true");
            }
            return SendAsync(request, cancellationToken);
        }

        private async Task<Reply> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            using var client = new HttpClient(_handlers.Create(_cookies)) { BaseAddress = _baseUri, Timeout = TimeSpan.FromSeconds(30) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; ClercqIt-MyZen/1.0; +https://www.clercq.it)");
            using (request)
            using (var response = await client.SendAsync(request, cancellationToken))
            {
                var finalPath = response.RequestMessage?.RequestUri?.AbsolutePath ?? string.Empty;
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                return new Reply(response.StatusCode, body,
                    finalPath.StartsWith(LogonPath, StringComparison.OrdinalIgnoreCase));
            }
        }

        public MyZenSessionData Snapshot(MyZenLoginPage? page) =>
            new(
                _cookies.GetAllCookies()
                    .Where(c => !c.Expired)
                    .Select(c => new MyZenCookie(c.Name, c.Value, c.Domain, c.Path,
                        c.Expires == DateTime.MinValue ? null : c.Expires.ToUniversalTime(), c.Secure, c.HttpOnly))
                    .ToList(),
                page?.FormToken ?? _data?.FormToken,
                page?.HeaderToken ?? _data?.HeaderToken,
                page?.CaptchaCheck ?? _data?.CaptchaCheck);
    }

    /// <summary>The result of a change; <c>Found</c> is false when MyZen had no entry with the ID.</summary>
    private sealed record Outcome(bool Found)
    {
        public static readonly Outcome Done = new(true);
        public static readonly Outcome NotFound = new(false);
    }

    /// <summary>A MyZen response; <c>IsLoginPage</c> when it ended on the login page, so the session is not (or no longer) signed in.</summary>
    private sealed record Reply(HttpStatusCode Status, string Body, bool IsLoginPage);
}
