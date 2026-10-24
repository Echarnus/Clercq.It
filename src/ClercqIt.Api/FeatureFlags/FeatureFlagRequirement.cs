using Microsoft.AspNetCore.Authorization;

namespace Clercq.It.Api.FeatureFlags;

/// <summary>
/// An authorization requirement that holds only while a feature flag is on. Used where
/// an endpoint filter cannot reach, such as MCP tools: a tool whose policy fails is
/// hidden from the tool list.
/// </summary>
public sealed record FeatureFlagRequirement(string Name) : IAuthorizationRequirement;

public sealed class FeatureFlagAuthorizationHandler(IFeatureFlags flags) : AuthorizationHandler<FeatureFlagRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, FeatureFlagRequirement requirement)
    {
        if (flags.IsEnabled(requirement.Name))
        {
            context.Succeed(requirement);
        }
        return Task.CompletedTask;
    }
}
