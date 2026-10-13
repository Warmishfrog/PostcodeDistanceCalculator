namespace PostcodeDistanceCalculator.Models;

/// <summary>
/// One driving leg between two consecutive postcodes in a journey.
/// </summary>
/// <param name="Start">Normalised start postcode (upper case, no spaces).</param>
/// <param name="End">Normalised end postcode.</param>
/// <param name="Meters">Driving distance in metres, as returned by the routing service. History saved by version
/// 2.1.0 or earlier holds figures scaled up by 7%.</param>
public sealed record Leg(string Start, string End, double Meters);
