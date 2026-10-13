using PostcodeDistanceCalculator.Models;

namespace PostcodeDistanceCalculator.Extensions;

/// <summary>
/// Conversion and naming helpers for <see cref="DistanceUnit"/>.
/// </summary>
public static class DistanceUnitExtensions
{
    /// <summary>Miles in one kilometre.</summary>
    public const double MilesPerKm = 0.621371;

    /// <summary>Converts a distance in kilometres into <paramref name="unit"/>.</summary>
    public static double FromKm(this DistanceUnit unit, double km)
        => unit == DistanceUnit.Miles ? km * MilesPerKm : km;

    /// <summary>Long unit name used in the breakdown, e.g. "miles".</summary>
    public static string LongName(this DistanceUnit unit)
        => unit == DistanceUnit.Miles ? "miles" : "kilometers";

    /// <summary>Short unit name used in labels, e.g. "mi".</summary>
    public static string ShortName(this DistanceUnit unit)
        => unit == DistanceUnit.Miles ? "mi" : "km";

    /// <summary>
    /// A distance converted into <paramref name="unit"/> and formatted to one decimal place in the
    /// current culture, with no unit suffix. This is exactly what gets copied to the clipboard.
    /// </summary>
    public static string FormatNumber(this DistanceUnit unit, double km)
        => unit.FromKm(km).ToString("F1");
}
