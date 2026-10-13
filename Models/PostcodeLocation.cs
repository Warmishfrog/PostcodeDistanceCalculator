namespace PostcodeDistanceCalculator.Models;

/// <summary>
/// Where a postcode is on the map, as returned by postcodes.io.
/// </summary>
/// <param name="Latitude">WGS84 latitude of the postcode's centre.</param>
/// <param name="Longitude">WGS84 longitude of the postcode's centre.</param>
/// <param name="IsTerminated">True when the postcode is no longer in use and this is its last known location.</param>
/// <param name="YearTerminated">Year the postcode was retired, when known.</param>
public sealed record PostcodeLocation(double Latitude, double Longitude, bool IsTerminated = false, int? YearTerminated = null);
