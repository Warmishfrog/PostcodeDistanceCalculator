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
    /// <summary>
    /// Total distance in kilometres that the user read from Google Maps to verify this journey, or null if not verified.
    /// When set it replaces <see cref="EstimatedKm"/> in every total. Optional, so history saved before it existed still loads.
    /// </summary>
    public double? GoogleMapsKm { get; init; }

    /// <summary>All stops in travel order: the first leg's start, then every leg's end.</summary>
    [JsonIgnore] public IReadOnlyList<string> Stops => Legs.Count == 0 ? [] : [Legs[0].Start, .. Legs.Select(l => l.End)];

    /// <summary>First postcode of the route, used as the journey's short label in history.</summary>
    [JsonIgnore] public string FirstPostcode => Legs.Count > 0 ? Legs[0].Start : string.Empty;

    /// <summary>The app's own estimate: the sum of the (calibrated) leg distances, in kilometres.</summary>
    [JsonIgnore] public double EstimatedKm => Legs.Sum(l => l.Meters) / 1000.0;

    /// <summary>Total distance in kilometres: the Google Maps figure if the journey was verified, otherwise the estimate.</summary>
    [JsonIgnore] public double TotalKm => GoogleMapsKm ?? EstimatedKm;

    /// <summary>True when the total comes from Google Maps rather than the app's estimate.</summary>
    [JsonIgnore] public bool IsVerified => GoogleMapsKm.HasValue;
}
