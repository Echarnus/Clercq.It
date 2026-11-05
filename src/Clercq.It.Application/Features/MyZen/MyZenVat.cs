using System.Globalization;
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

    /// <summary>
    /// A VAT number the way MyZen stores it: upper case, no spaces, dots or dashes. A bare
    /// Belgian company number (<c>0825.170.684</c>) gets the <c>BE</c> prefix.
    /// </summary>
    public static string Normalize(string vat)
    {
        var compact = Separators().Replace(vat, string.Empty).ToUpperInvariant();
        return BelgianDigits().IsMatch(compact) ? "BE" + compact : compact;
    }

    /// <summary>
    /// Whether a normalized VAT number has a valid shape. Belgian numbers (<c>BE</c> plus ten digits,
    /// starting with 0 or 1) must also pass the modulo-97 check; other countries only need a
    /// two-letter prefix and 2 to 13 letters or digits.
    /// </summary>
    public static bool IsValid(string normalized)
    {
        if (normalized.StartsWith("BE", StringComparison.Ordinal))
        {
            if (!Belgian().IsMatch(normalized))
            {
                return false;
            }

            var number = long.Parse(normalized.AsSpan(2, 8), NumberStyles.None, CultureInfo.InvariantCulture);
            var check = int.Parse(normalized.AsSpan(10, 2), NumberStyles.None, CultureInfo.InvariantCulture);
            return 97 - (number % 97) == check;
        }

        return Foreign().IsMatch(normalized);
    }

    [GeneratedRegex(@"[\s.\-]")]
    private static partial Regex Separators();

    [GeneratedRegex("^[A-Za-z]{0,2}[0-9]{8,12}$")]
    private static partial Regex VatLike();

    [GeneratedRegex("^[01][0-9]{9}$")]
    private static partial Regex BelgianDigits();

    [GeneratedRegex("^BE[01][0-9]{9}$")]
    private static partial Regex Belgian();

    [GeneratedRegex("^[A-Z]{2}[0-9A-Z]{2,13}$")]
    private static partial Regex Foreign();
}
