using System.Text.Json.Serialization;

namespace PostcodeDistanceCalculator.Models;

/// <summary>
/// The journeys produced by one Calculate run, saved to history together.
/// Every batch is stamped with when it was calculated, so saved history can be grouped and dated
/// even when the user leaves the optional per-journey date blank.
/// </summary>
public sealed class JourneyBatch
{
    /// <summary>Local date and time the batch was calculated. Always set.</summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>Journeys in the order they were entered. Mutable so single journeys can be deleted from history.</summary>
    public List<Journey> Journeys { get; init; } = [];

    /// <summary>Combined driving distance of every journey in the batch, in kilometres.</summary>
    [JsonIgnore] public double TotalKm => Journeys.Sum(j => j.TotalKm);
}
