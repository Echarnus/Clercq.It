using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Clercq.It.ContentImport;

/// <summary>
/// A Markdown file with YAML front matter, e.g. content/projects/clercqit.md.
/// The slug is the file name without extension.
/// </summary>
public sealed record ContentFile(string Slug, string Path, IReadOnlyDictionary<string, object?> FrontMatter, string Body)
{
    private static readonly IDeserializer Yaml = new DeserializerBuilder()
        .WithNamingConvention(NullNamingConvention.Instance)
        .Build();

    public static ContentFile Parse(string path, string text)
    {
        var slug = System.IO.Path.GetFileNameWithoutExtension(path);
        text = text.Replace("\r\n", "\n");

        if (!text.StartsWith("---\n"))
        {
            throw new ContentException(path, "file must start with a '---' front matter block");
        }

        var end = text.IndexOf("\n---", 4, StringComparison.Ordinal);
        if (end < 0)
        {
            throw new ContentException(path, "front matter block is not closed with '---'");
        }

        var yaml = text[4..end];
        var bodyStart = text.IndexOf('\n', end + 1);
        var body = bodyStart < 0 ? string.Empty : text[(bodyStart + 1)..].Trim();

        Dictionary<string, object?> frontMatter;
        try
        {
            frontMatter = Yaml.Deserialize<Dictionary<string, object?>>(yaml) ?? new();
        }
        catch (YamlDotNet.Core.YamlException ex)
        {
            throw new ContentException(path, $"invalid front matter: {ex.Message}");
        }

        return new ContentFile(slug, path, frontMatter, body);
    }

    public string RequiredString(string key) =>
        OptionalString(key) is { Length: > 0 } value
            ? value
            : throw new ContentException(Path, $"'{key}' is required");

    public string? OptionalString(string key) =>
        FrontMatter.TryGetValue(key, out var value) ? value?.ToString()?.Trim() : null;

    public bool OptionalBool(string key, bool fallback = false)
    {
        var value = OptionalString(key);
        if (string.IsNullOrEmpty(value)) return fallback;
        return bool.TryParse(value, out var result)
            ? result
            : throw new ContentException(Path, $"'{key}' must be true or false");
    }

    public DateTime RequiredDate(string key) =>
        OptionalDate(key) ?? throw new ContentException(Path, $"'{key}' is required (yyyy-MM-dd)");

    public DateTime? OptionalDate(string key)
    {
        var value = OptionalString(key);
        if (string.IsNullOrEmpty(value)) return null;
        return DateTime.TryParseExact(value, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal,
                out var date)
            ? DateTime.SpecifyKind(date, DateTimeKind.Utc)
            : throw new ContentException(Path, $"'{key}' must be a date in yyyy-MM-dd format");
    }

    public string[] StringList(string key)
    {
        if (!FrontMatter.TryGetValue(key, out var value) || value is null) return [];
        if (value is IEnumerable<object> items)
        {
            return items.Select(i => i?.ToString()?.Trim() ?? string.Empty).Where(i => i.Length > 0).ToArray();
        }
        throw new ContentException(Path, $"'{key}' must be a list");
    }
}

public sealed class ContentException(string path, string message) : Exception($"{path}: {message}");
