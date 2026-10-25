using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Clercq.It.Api.FeatureFlags;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;

namespace ClercqIt.Api.Tests;

public class ConfigurationFeatureFlagsTests
{
    private static ConfigurationFeatureFlags Flags(params (string Name, string Value)[] flags) =>
        new(new ConfigurationBuilder()
            .AddInMemoryCollection(flags.Select(f => new KeyValuePair<string, string?>($"FeatureFlags:{f.Name}", f.Value)))
            .Build());

    [Theory(DisplayName = "A flag is on only when it is set to true")]
    [InlineData("true", true)]
    [InlineData("True", true)]
    [InlineData("false", false)]
    [InlineData("yes", false)]
    [InlineData("", false)]
    public void IsEnabled_ConfiguredValue_ParsesAsBool(string value, bool expected)
    {
        // Arrange
        var flags = Flags(("Search", value));

        // Act & Assert
        flags.IsEnabled("Search").Should().Be(expected);
    }

    [Fact(DisplayName = "An unknown flag is off and names are case-insensitive")]
    public void IsEnabled_UnknownOrDifferentCase_OffOrMatched()
    {
        // Arrange
        var flags = Flags(("Search", "true"));

        // Act & Assert
        flags.IsEnabled("Unknown").Should().BeFalse();
        flags.IsEnabled("search").Should().BeTrue();
    }

    [Fact(DisplayName = "All returns every configured flag")]
    public void All_ConfiguredFlags_ReturnsEach()
    {
        // Arrange
        var flags = Flags(("Search", "true"), ("NewBlogEditor", "false"));

        // Act
        var all = flags.All();

        // Assert
        all.Should().BeEquivalentTo(new Dictionary<string, bool> { ["Search"] = true, ["NewBlogEditor"] = false });
    }
}

public class FeatureFlagAuthorizationHandlerTests
{
    [Theory(DisplayName = "A feature flag requirement holds only while the flag is on")]
    [InlineData("true", true)]
    [InlineData("false", false)]
    public async Task Requirement_FollowsFlag(string value, bool succeeds)
    {
        // Arrange
        var flags = new ConfigurationFeatureFlags(new ConfigurationBuilder()
            .AddInMemoryCollection([new KeyValuePair<string, string?>("FeatureFlags:MyZen", value)])
            .Build());
        var requirement = new FeatureFlagRequirement("MyZen");
        var context = new AuthorizationHandlerContext([requirement], new ClaimsPrincipal(), resource: null);

        // Act
        await new FeatureFlagAuthorizationHandler(flags).HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().Be(succeeds);
    }
}

public class RequireFeatureTests
{
    private static async Task<HttpClient> ClientAsync(string? flagValue)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        if (flagValue is not null)
        {
            builder.Configuration["FeatureFlags:Preview"] = flagValue;
        }
        builder.Services.AddFeatureFlags();
        var app = builder.Build();
        app.MapGet("/preview", () => "hello").RequireFeature("Preview");
        app.MapGroup("/group").RequireFeature("Preview").MapGet("/item", () => "item");
        await app.StartAsync();
        return app.GetTestClient();
    }

    [Theory(DisplayName = "A flagged endpoint is 404 while its flag is off")]
    [InlineData(null)]
    [InlineData("false")]
    public async Task RequireFeature_FlagOff_ReturnsNotFound(string? flagValue)
    {
        // Arrange
        var client = await ClientAsync(flagValue);

        // Act
        var endpoint = await client.GetAsync("/preview");
        var group = await client.GetAsync("/group/item");

        // Assert
        endpoint.StatusCode.Should().Be(HttpStatusCode.NotFound);
        group.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "A flagged endpoint works when its flag is on")]
    public async Task RequireFeature_FlagOn_RunsEndpoint()
    {
        // Arrange
        var client = await ClientAsync("true");

        // Act
        var endpoint = await client.GetStringAsync("/preview");
        var group = await client.GetStringAsync("/group/item");

        // Assert
        endpoint.Should().Be("hello");
        group.Should().Be("item");
    }
}

[Collection(ApiTestGroup.Name)]
public class FeatureFlagEndpointTests(CustomWebApplicationFactory factory)
{
    [Fact(DisplayName = "GET /api/features returns no flags in production by default")]
    public async Task GetFeatures_NoFlagsConfigured_ReturnsEmpty()
    {
        // Arrange
        var client = await factory.CreateClientForUserAsync("admin");

        // Act
        var flags = await client.GetFromJsonAsync<Dictionary<string, bool>>("/api/features");

        // Assert
        flags.Should().BeEmpty();
    }

    [Fact(DisplayName = "GET /api/features returns the flags set by environment or config")]
    public async Task GetFeatures_FlagsConfigured_ReturnsThem()
    {
        // Arrange
        using var app = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("FeatureFlags:Search", "true");
            builder.UseSetting("FeatureFlags:NewBlogEditor", "false");
        });
        var client = app.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", await factory.GetTokenAsync("blogger"));

        // Act
        var flags = await client.GetFromJsonAsync<Dictionary<string, bool>>("/api/features");

        // Assert
        flags.Should().BeEquivalentTo(new Dictionary<string, bool> { ["Search"] = true, ["NewBlogEditor"] = false });
    }

    [Fact(DisplayName = "GET /api/features without a token returns 401, so visitors do not learn the flag names")]
    public async Task GetFeatures_Anonymous_ReturnsUnauthorized()
    {
        // Act
        var response = await factory.CreateClient().GetAsync("/api/features");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "GET /api/features for a user without an admin role returns 403")]
    public async Task GetFeatures_UserWithoutRoles_ReturnsForbidden()
    {
        // Arrange
        var client = await factory.CreateClientForUserAsync("test");

        // Act
        var response = await client.GetAsync("/api/features");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
