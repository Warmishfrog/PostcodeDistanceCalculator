using System.Globalization;
using System.Text.RegularExpressions;

namespace PostcodeDistanceCalculator.Helpers;

/// <summary>
/// Reads a distance the user typed or pasted from Google Maps, e.g. "210", "210.4", "210,4", "1,204 km" or
/// "25 min (12.3 miles)". Only the number is used; the unit is whatever unit the app is currently showing.
/// </summary>
public static partial class DistanceInput
{
    // A number directly followed by a distance unit, so "25 min (12.3 miles)" gives 12.3 rather than the time
    [GeneratedRegex(@"(\d[\d,.]*)\s*(?:mi|mile|miles|km|kilometres|kilometers)\b", RegexOptions.IgnoreCase)]
    private static partial Regex NumberWithUnitPattern();

    [GeneratedRegex(@"\d[\d,.]*")]
    private static partial Regex NumberPattern();

    // A comma followed by one or two digits at the end ("210,4") is a decimal comma, not a thousands separator ("1,204")
    [GeneratedRegex(@"^\d+,\d{1,2}$")]
    private static partial Regex DecimalCommaPattern();

    /// <summary>
    /// Parses the distance in <paramref name="text"/>: the number next to "mi", "miles" or "km" if there is one,
    /// otherwise the first number. Returns false for no number or a distance that isn't greater than zero.
    /// </summary>
    public static bool TryParse(string text, out double distance)
    {
        distance = 0;

        var withUnit = NumberWithUnitPattern().Match(text);
        var number = withUnit.Success ? withUnit.Groups[1] : NumberPattern().Match(text);
        if (!number.Success) return false;

        string value = number.Value.TrimEnd('.', ',');
        value = DecimalCommaPattern().IsMatch(value) ? value.Replace(',', '.') : value.Replace(",", "");

        return double.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out distance)
            && distance > 0;
    }

    /// <summary>
    /// Formats a distance for the user to edit, always with a '.' decimal point so <see cref="TryParse"/> reads it back
    /// exactly, whatever the Windows number format.
    /// </summary>
    public static string Format(double distance) => distance.ToString("0.0", CultureInfo.InvariantCulture);
}
