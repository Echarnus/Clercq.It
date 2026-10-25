using Clercq.It.Api.Features;
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
    /// <c>GET /api/features</c>: the configured flags, so the admin UI can hide
    /// pages and links. Admins only (issue #454): flag names say what is being built,
    /// so anonymous visitors do not get them. Never use a flag as access control.
    /// </summary>
    public static IEndpointRouteBuilder MapFeatureFlagEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/features", (IFeatureFlags flags) => Results.Ok(flags.All()))
            .WithTags("Feature flags")
            .WithName("GetFeatureFlags")
            .WithSummary("Get the feature flags")
            .WithDescription("Returns every configured feature flag and whether it is on; needs an admin role")
            .RequireRateLimiting("api")
            .RequireAuthorization(AuthEndpoints.AdminAreaPolicy);
        return endpoints;
    }
}
