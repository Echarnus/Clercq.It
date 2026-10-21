namespace Clercq.It.Api.FeatureFlags;

/// <summary>
/// Config-driven feature flags (issue #253). A flag is on only when the
/// <c>FeatureFlags</c> configuration section sets it to <c>true</c>, for example
/// with the environment variable <c>FeatureFlags__NewBlogEditor=true</c>.
/// Unknown or unparsable flags are off. See docs/versioning.md#feature-flags.
/// </summary>
public interface IFeatureFlags
{
    /// <summary>Whether the flag is on. Names are case-insensitive.</summary>
    bool IsEnabled(string name);

    /// <summary>Every configured flag and whether it is on.</summary>
    IReadOnlyDictionary<string, bool> All();
}
