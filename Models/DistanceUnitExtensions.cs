namespace PostcodeDistanceCalculator.Models;

public static class DistanceUnitExtensions
{
    public const double MilesPerKm = 0.621371;

    public static double FromKm(this DistanceUnit unit, double km)
        => unit == DistanceUnit.Miles ? km * MilesPerKm : km;

    public static double ToKm(this DistanceUnit unit, double value)
        => unit == DistanceUnit.Miles ? value / MilesPerKm : value;

    public static string LongName(this DistanceUnit unit)
        => unit == DistanceUnit.Miles ? "miles" : "kilometers";

    public static string ShortName(this DistanceUnit unit)
        => unit == DistanceUnit.Miles ? "mi" : "km";

    /// <summary>
    /// A distance converted into <paramref name="unit"/> and formatted to one decimal place in the
    /// current culture, with no unit suffix. This is exactly what gets copied to the clipboard.
    /// </summary>
    public static string FormatNumber(this DistanceUnit unit, double km)
        => unit.FromKm(km).ToString("F1");
}
