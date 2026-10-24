using Microsoft.AspNetCore.Authorization;

namespace Clercq.It.Api.FeatureFlags;

public static class FeatureFlagExtensions
{
    public static IServiceCollection AddFeatureFlags(this IServiceCollection services) =>
        services
            .AddSingleton<IFeatureFlags, ConfigurationFeatureFlags>()
            .AddSingleton<IAuthorizationHandler, FeatureFlagAuthorizationHandler>();

    /// <summary>
    /// Answers 404 Not Found while the flag is off, as if the endpoint did not
    /// exist. Works on a single endpoint or on a whole group.
    /// </summary>
    public static TBuilder RequireFeature<TBuilder>(this TBuilder builder, string name)
        where TBuilder : IEndpointConventionBuilder =>
        builder.AddEndpointFilter(async (context, next) =>
            context.HttpContext.RequestServices.GetRequiredService<IFeatureFlags>().IsEnabled(name)
                ? await next(context)
                : Results.NotFound());

    /// <summary>
    /// <c>GET /api/features</c>: the configured flags, so the frontend can hide
    /// pages and links. Flags are not secret; never use one as access control.
    /// </summary>
    public static IEndpointRouteBuilder MapFeatureFlagEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/features", (IFeatureFlags flags) => Results.Ok(flags.All()))
            .WithTags("Feature flags")
            .WithName("GetFeatureFlags")
            .WithSummary("Get the feature flags")
            .WithDescription("Returns every configured feature flag and whether it is on")
            .RequireRateLimiting("api");
        return endpoints;
    }
}
