using System.Globalization;
using System.Text.RegularExpressions;

namespace PostcodeDistanceCalculator.Helpers;

/// <summary>
/// Reads a distance the user typed or pasted from Google Maps, e.g. "210", "210.4", "210.4 mi" or "1,204 km".
/// Only the number is used; the unit is whatever unit the app is currently showing.
/// </summary>
public static partial class DistanceInput
{
    [GeneratedRegex(@"\d[\d,]*(?:\.\d+)?")]
    private static partial Regex NumberPattern();

    /// <summary>
    /// Parses the first number in <paramref name="text"/>, ignoring thousands separators and any unit text.
    /// Returns false for empty text, no number, or a distance that isn't greater than zero.
    /// </summary>
    public static bool TryParse(string text, out double distance)
    {
        distance = 0;
        var match = NumberPattern().Match(text);
        if (!match.Success) return false;

        // Google Maps shows UK distances with '.' decimals and ',' thousands, so parse invariantly
        return double.TryParse(match.Value.Replace(",", ""), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out distance)
            && distance > 0;
    }
}
