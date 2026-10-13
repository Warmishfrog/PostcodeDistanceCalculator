using System.Text.Json.Serialization;

namespace PostcodeDistanceCalculator.Models;

/// <summary>
/// A single route of two or more postcodes, e.g. one trip on one day.
/// Totals are derived from <see cref="Legs"/> rather than stored, so they can never drift out of sync.
/// </summary>
/// <param name="DateTag">Optional free-text date the user typed (e.g. "24/09"); empty when not given.</param>
/// <param name="RawInput">The text the user typed for this journey, used to restore the input box.</param>
/// <param name="Legs">Legs in travel order.</param>
public sealed record Journey(string DateTag, string RawInput, IReadOnlyList<Leg> Legs)
{
    /// <summary>First postcode of the route, used as the journey's short label in history.</summary>
    [JsonIgnore] public string FirstPostcode => Legs.Count > 0 ? Legs[0].Start : string.Empty;

    /// <summary>Total driving distance in kilometres.</summary>
    [JsonIgnore] public double TotalKm => Legs.Sum(l => l.Meters) / 1000.0;
}
