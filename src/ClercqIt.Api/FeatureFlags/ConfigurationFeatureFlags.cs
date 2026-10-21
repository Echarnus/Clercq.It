namespace Clercq.It.Api.FeatureFlags;

/// <summary>
/// Reads flags from the <c>FeatureFlags</c> configuration section on every call,
/// so a changed environment variable takes effect on the next container revision.
/// </summary>
public sealed class ConfigurationFeatureFlags(IConfiguration configuration) : IFeatureFlags
{
    public const string SectionName = "FeatureFlags";

    public bool IsEnabled(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return bool.TryParse(configuration[$"{SectionName}:{name}"], out var enabled) && enabled;
    }

    public IReadOnlyDictionary<string, bool> All() =>
        configuration.GetSection(SectionName).GetChildren()
            .ToDictionary(flag => flag.Key, flag => bool.TryParse(flag.Value, out var enabled) && enabled,
                StringComparer.OrdinalIgnoreCase);
}
