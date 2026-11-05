namespace Clercq.It.Application.Features.MyZen;

/// <summary>
/// MyZen's project statuses: the value its edit form posts and the label its pages show
/// (Prospect, Bezig, Klaar, Geannuleerd).
/// </summary>
public static class MyZenProjectStatus
{
    public const string Prospect = "Prospect";
    public const string InProgress = "InProgress";
    public const string Finished = "Finished";
    public const string Canceled = "Canceled";

    private static readonly Dictionary<string, string> Labels = new(StringComparer.Ordinal)
    {
        [Prospect] = "Prospect",
        [InProgress] = "Bezig",
        [Finished] = "Klaar",
        [Canceled] = "Geannuleerd",
    };

    private static readonly Dictionary<string, string> Values = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Prospect"] = Prospect,
        ["Bezig"] = InProgress,
        ["InProgress"] = InProgress,
        ["In progress"] = InProgress,
        ["Klaar"] = Finished,
        ["Finished"] = Finished,
        ["Geannuleerd"] = Canceled,
        ["Canceled"] = Canceled,
        ["Cancelled"] = Canceled,
    };

    /// <summary>Accepted in tools: the Dutch labels and the English form values.</summary>
    public const string Accepted = "Prospect, Bezig, Klaar or Geannuleerd";

    /// <summary>The form value for a label or value, or null when it is not a status.</summary>
    public static string? ToValue(string? status) =>
        status is not null && Values.TryGetValue(status.Trim(), out var value) ? value : null;

    /// <summary>The label MyZen shows for a form value; an unknown value comes back as it is.</summary>
    public static string ToLabel(string value) => Labels.TryGetValue(value, out var label) ? label : value;
}
