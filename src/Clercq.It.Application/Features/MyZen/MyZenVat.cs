using System.Text.RegularExpressions;

namespace Clercq.It.Application.Features.MyZen;

/// <summary>
/// VAT and company numbers as MyZen stores them: country prefix and digits only
/// (<c>BE0825170684</c>). People type them as <c>BE 0825.170.684</c>.
/// </summary>
public static partial class MyZenVat
{
    /// <summary>The query MyZen's customer search expects: a typed VAT or company number without spaces, dots or dashes; anything else trimmed.</summary>
    public static string SearchKeyword(string query)
    {
        var trimmed = query.Trim();
        var compact = Separators().Replace(trimmed, string.Empty);
        return VatLike().IsMatch(compact) ? compact.ToUpperInvariant() : trimmed;
    }

    [GeneratedRegex(@"[\s.\-]")]
    private static partial Regex Separators();

    [GeneratedRegex("^[A-Za-z]{0,2}[0-9]{8,12}$")]
    private static partial Regex VatLike();
}
