using System.Text.Json.Serialization;

namespace PostcodeDistanceCalculator.Models;

/// <summary>
/// A single route of two or more postcodes, e.g. one trip on one day.
/// Totals are derived from <see cref="Legs"/> rather than stored, so they can never drift out of sync.
/// </summary>
/// <param name="DateTag">Optional free-text date the user typed (e.g. "24/09"); empty when not given.</param>
/// <param name="Legs">Legs in travel order.</param>
public sealed record Journey(string DateTag, IReadOnlyList<Leg> Legs)
{
    /// <summary>
    /// Total distance in kilometres that the user read from Google Maps to verify this journey, or null if not verified.
    /// When set it replaces <see cref="EstimatedKm"/> in every total. Optional, so history saved before it existed still loads.
    /// </summary>
    public double? GoogleMapsKm { get; init; }

    [JsonIgnore] public IReadOnlyList<string> Stops => Legs.Count == 0 ? [] : [Legs[0].Start, .. Legs.Select(l => l.End)];

    [JsonIgnore] public double EstimatedKm => Legs.Sum(l => l.Meters) / 1000.0;

    [JsonIgnore] public double TotalKm => GoogleMapsKm ?? EstimatedKm;

    [JsonIgnore] public bool IsVerified => GoogleMapsKm.HasValue;
}
