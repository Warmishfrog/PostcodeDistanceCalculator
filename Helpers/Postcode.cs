using System.Text.RegularExpressions;

namespace PostcodeDistanceCalculator.Helpers;

/// <summary>UK postcode matching and formatting shared by the parser, the typo warnings and the UI.</summary>
public static partial class Postcode
{
    [GeneratedRegex(@"[A-Z]{1,2}[0-9][A-Z0-9]?\s*[0-9][A-Z]{2}", RegexOptions.IgnoreCase)]
    internal static partial Regex InText();

    // One or more whole postcodes and nothing else, e.g. "BS14 0SL BS15 8DB" typed without a comma
    [GeneratedRegex(@"^(\s*[A-Z]{1,2}[0-9][A-Z0-9]?\s*[0-9][A-Z]{2})+\s*$", RegexOptions.IgnoreCase)]
    internal static partial Regex RunTogether();

    /// <summary>
    /// Removes all whitespace (including tabs and non-breaking spaces pasted from spreadsheets) and upper-cases
    /// with the invariant culture, so "sw1a 1aa" becomes "SW1A1AA" under every Windows locale.
    /// </summary>
    public static string Normalize(string postcode)
        => string.Concat(postcode.Where(c => !char.IsWhiteSpace(c))).ToUpperInvariant();

    /// <summary>Puts back the space before the 3-character inward code, e.g. "SW1A1AA" becomes "SW1A 1AA".</summary>
    public static string Format(string postcode)
        => postcode.Length > 3 && !postcode.Contains(' ') ? $"{postcode[..^3]} {postcode[^3..]}" : postcode;
}
