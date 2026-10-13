namespace PostcodeDistanceCalculator.Models;

/// <summary>
/// One driving leg between two consecutive postcodes in a journey.
/// </summary>
/// <param name="Start">Normalised start postcode (upper case, no spaces).</param>
/// <param name="End">Normalised end postcode.</param>
/// <param name="Meters">Driving distance in metres, already scaled by <see cref="Services.RouteApi.CalibrationFactor"/>.</param>
public sealed record Leg(string Start, string End, double Meters);
