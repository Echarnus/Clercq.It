using System.Net;
using System.Net.Http.Json;
using Clercq.It.Application.Features.Statistics;
using FluentAssertions;

namespace ClercqIt.Api.Tests.Statistics;

/// <summary>The /api/stats endpoints against the real database and Keycloak.</summary>
[Collection(ApiTestGroup.Name)]
public class StatisticsEndpointTests(CustomWebApplicationFactory factory) : IAsyncLifetime
{
    private const string Browser = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/131.0 Safari/537.36";

    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "Page views are counted per visitor and shown to an admin")]
    public async Task PageViews_CountedPerVisitor_ShownInStatistics()
    {
        // Arrange
        var anonymous = factory.CreateClient();

        // Act
        (await PostPageViewAsync(anonymous, "/", "203.0.113.7", Browser)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        await PostPageViewAsync(anonymous, "/", "203.0.113.7", Browser);
        await PostPageViewAsync(anonymous, "/blogs", "203.0.113.7", Browser);
        await PostPageViewAsync(anonymous, "/", "198.51.100.4", Browser);
        var admin = await factory.CreateClientForUserAsync("admin");
        var stats = await admin.GetFromJsonAsync<SiteStatisticsDto>("/api/stats?days=7");

        // Assert
        stats!.Days.Should().HaveCount(7);
        stats.PageViews.Should().Be(4);
        stats.UniquePageViews.Should().Be(3);
        stats.Visitors.Should().Be(2);
        stats.TopPages.Should().BeEquivalentTo(
            [new PageStatisticsDto("/", 3, 2), new PageStatisticsDto("/blogs", 1, 1)],
            options => options.WithStrictOrdering());
    }

    [Fact(DisplayName = "Bots and admin pages are accepted but not counted")]
    public async Task PageViews_BotOrAdmin_NotCounted()
    {
        // Arrange
        var anonymous = factory.CreateClient();

        // Act
        var bot = await PostPageViewAsync(anonymous, "/", "203.0.113.7", "Mozilla/5.0 (compatible; bingbot/2.0)");
        await PostPageViewAsync(anonymous, "/admin/dashboard", "203.0.113.7", Browser);
        var admin = await factory.CreateClientForUserAsync("admin");
        var stats = await admin.GetFromJsonAsync<SiteStatisticsDto>("/api/stats");

        // Assert
        bot.StatusCode.Should().Be(HttpStatusCode.NoContent);
        stats!.Days.Should().HaveCount(30);
        stats.PageViews.Should().Be(0);
    }

    [Fact(DisplayName = "A page view with an invalid path returns 400")]
    public async Task PageView_InvalidPath_ReturnsBadRequest()
    {
        // Act
        var response = await PostPageViewAsync(factory.CreateClient(), "https://example.com/?q=1", "203.0.113.7", Browser);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "Statistics for an invalid number of days return 400")]
    public async Task GetStatistics_InvalidDays_ReturnsBadRequest()
    {
        // Arrange
        var admin = await factory.CreateClientForUserAsync("admin");

        // Act
        var response = await admin.GetAsync("/api/stats?days=0");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "Statistics without a token return 401")]
    public async Task GetStatistics_Anonymous_ReturnsUnauthorized()
    {
        // Act
        var response = await factory.CreateClient().GetAsync("/api/stats");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "Statistics for a user without Admin.View return 403")]
    public async Task GetStatistics_UserWithoutAdminView_ReturnsForbidden()
    {
        // Arrange
        var client = await factory.CreateClientForUserAsync("test");

        // Act
        var response = await client.GetAsync("/api/stats");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static Task<HttpResponseMessage> PostPageViewAsync(HttpClient client, string path, string ip, string userAgent)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/stats/pageview")
        {
            Content = JsonContent.Create(new { path }),
        };
        request.Headers.Add("X-Forwarded-For", $"{ip}, 10.0.0.1");
        request.Headers.TryAddWithoutValidation("User-Agent", userAgent);
        return client.SendAsync(request);
    }
}
