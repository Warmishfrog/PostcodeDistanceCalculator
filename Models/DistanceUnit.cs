namespace PostcodeDistanceCalculator.Models;

/// <summary>
/// Unit distances are displayed and copied in. Distances are always stored in kilometres/metres
/// and only converted for display (see <see cref="Extensions.DistanceUnitExtensions"/>),
/// so switching units never needs to re-query the APIs.
/// </summary>
public enum DistanceUnit
{
    Miles,
    Kilometers
}
