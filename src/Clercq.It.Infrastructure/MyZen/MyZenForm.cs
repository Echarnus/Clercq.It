using System.Globalization;
using System.Text.Json;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;

namespace Clercq.It.Infrastructure.MyZen;

/// <summary>
/// One of MyZen's edit forms, read like a browser submits it (issue #495): every field the
/// page holds, so a save keeps the values the tools do not know about. The tools then change
/// only the fields they were given.
/// </summary>
public sealed class MyZenForm
{
    private static readonly HtmlParser Parser = new();
    private static readonly string[] SkippedTypes = ["submit", "button", "image", "file", "reset"];

    private readonly List<KeyValuePair<string, string>> _fields;

    private MyZenForm(List<KeyValuePair<string, string>> fields, string? saveAction, string? token)
    {
        _fields = fields;
        SaveAction = saveAction;
        Token = token;
    }

    /// <summary>The fields in document order, as the browser would post them.</summary>
    public IReadOnlyList<KeyValuePair<string, string>> Fields => _fields;

    /// <summary>The <c>buttonactiontype</c> of the page's "Opslaan" button (MyZen posts it as <c>ButtonActionType</c>).</summary>
    public string? SaveAction { get; }

    /// <summary>
    /// The anti-forgery token MyZen's scripts add to the form: the hidden input in the
    /// <c>csrfToken</c> of <c>script#layoutmainconfig</c>.
    /// </summary>
    public string? Token { get; }

    /// <summary>Reads <c>form#pageform</c>; null when the page has none.</summary>
    public static MyZenForm? Parse(string html)
    {
        var document = Parser.ParseDocument(html);
        var form = document.QuerySelector("form#pageform");
        if (form is null)
        {
            return null;
        }

        var fields = new List<KeyValuePair<string, string>>();
        foreach (var element in form.QuerySelectorAll("input[name], select[name], textarea[name]"))
        {
            var name = element.GetAttribute("name")!;
            if (name.Length == 0 || element.HasAttribute("disabled"))
            {
                continue;
            }

            switch (element.LocalName)
            {
                case "textarea":
                    fields.Add(KeyValuePair.Create(name, element.TextContent));
                    break;
                case "select":
                    var options = element.QuerySelectorAll("option").ToList();
                    var selected = options.Where(o => o.HasAttribute("selected")).ToList();
                    if (selected.Count == 0 && !element.HasAttribute("multiple") && options.Count > 0)
                    {
                        selected.Add(options[0]);
                    }
                    fields.AddRange(selected.Select(o => KeyValuePair.Create(name, o.GetAttribute("value") ?? o.TextContent.Trim())));
                    break;
                default:
                    var type = (element.GetAttribute("type") ?? "text").ToLowerInvariant();
                    if (SkippedTypes.Contains(type))
                    {
                        break;
                    }
                    if (type is "checkbox" or "radio" && !element.HasAttribute("checked"))
                    {
                        break;
                    }
                    fields.Add(KeyValuePair.Create(name, element.GetAttribute("value") ?? (type is "checkbox" or "radio" ? "on" : string.Empty)));
                    break;
            }
        }

        var save = document.QuerySelectorAll(".btnSubmitActionType[buttonactiontype]")
            .FirstOrDefault(b => b.TextContent.Trim().Equals("Opslaan", StringComparison.OrdinalIgnoreCase))
            ?.GetAttribute("buttonactiontype");

        return new MyZenForm(fields, save, ParseToken(document) ?? form.QuerySelector("input[name=__RequestVerificationToken]")?.GetAttribute("value"));
    }

    /// <summary>The current value of a field (its first occurrence), or null when the form has none.</summary>
    public string? Get(string name) =>
        _fields.FindIndex(f => f.Key == name) is var i and >= 0 ? _fields[i].Value : null;

    /// <summary>Sets every occurrence of a field (MyZen repeats some, such as the street); appends it when the form has none.</summary>
    public MyZenForm Set(string name, string value)
    {
        var found = false;
        for (var i = 0; i < _fields.Count; i++)
        {
            if (_fields[i].Key == name)
            {
                _fields[i] = KeyValuePair.Create(name, value);
                found = true;
            }
        }
        if (!found)
        {
            _fields.Add(KeyValuePair.Create(name, value));
        }
        return this;
    }

    /// <summary>Sets a field only when a value was given.</summary>
    public MyZenForm SetIfGiven(string name, string? value) => value is null ? this : Set(name, value.Trim());

    /// <summary>
    /// Sets an ASP.NET MVC checkbox: a checked box posts <c>true</c> followed by its hidden
    /// <c>false</c>, an unchecked one only the hidden <c>false</c>.
    /// </summary>
    public MyZenForm SetCheckbox(string name, bool? value)
    {
        if (value is not { } on)
        {
            return this;
        }

        var at = _fields.FindIndex(f => f.Key == name);
        _fields.RemoveAll(f => f.Key == name);
        var values = on ? new[] { "true", "false" } : ["false"];
        _fields.InsertRange(at < 0 ? _fields.Count : at, values.Select(v => KeyValuePair.Create(name, v)));
        return this;
    }

    /// <summary>A date the way MyZen's forms write it.</summary>
    public static string Date(DateOnly date) => date.ToString(MyZenHtml.DateFormat, CultureInfo.InvariantCulture);

    /// <summary>An amount the way MyZen's forms write it (<c>1000,50</c>).</summary>
    public static string Amount(decimal amount) =>
        amount.ToString("0.00", CultureInfo.InvariantCulture).Replace('.', ',');

    private static string? ParseToken(IDocument document)
    {
        var config = document.QuerySelector("script#layoutmainconfig")?.TextContent;
        if (string.IsNullOrWhiteSpace(config))
        {
            return null;
        }

        try
        {
            using var json = JsonDocument.Parse(config);
            return json.RootElement.ValueKind == JsonValueKind.Object
                && json.RootElement.TryGetProperty("csrfToken", out var input)
                && input.ValueKind == JsonValueKind.String
                && Parser.ParseDocument(input.GetString()!).QuerySelector("input")?.GetAttribute("value") is { Length: > 0 } token
                    ? token
                    : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
