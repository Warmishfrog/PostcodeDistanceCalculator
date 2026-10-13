using PostcodeDistanceCalculator.Extensions;
using PostcodeDistanceCalculator.Models;

namespace PostcodeDistanceCalculator.Helpers;

/// <summary>
/// Builds the plain-text "Journey Breakdown" shown in the results box.
/// A single journey gets a "ROUTE BREAKDOWN" heading; several journeys are numbered and followed by a combined total.
/// Journeys verified against Google Maps show Google's total, noting the app's own estimate beneath it.
/// </summary>
public static class BreakdownFormatter
{
    private const int RuleWidth = 45;

    /// <summary>Warning shown under any journey total that uses a figure read from Google Maps.</summary>
    public const string GoogleMapsCaveat = "Google Maps distances can change with circumstances such as traffic, roadworks or closures.";

    /// <summary>Formats <paramref name="journeys"/> in <paramref name="unit"/>. Returns an empty string for no journeys.</summary>
    /// <param name="compact">One line of legs per journey, e.g. "A  ->  B (0.7 mi), B  ->  C (3.4 mi)", instead of one leg per line.</param>
    /// <param name="showLegDistances">Show each leg's distance, not just the totals.</param>
    /// <param name="showEstimate">For journeys verified with Google Maps, also show the app's own estimate.</param>
    public static string Format(IReadOnlyList<Journey> journeys, DistanceUnit unit, bool compact = false, bool showLegDistances = true, bool showEstimate = true)
    {
        if (journeys.Count == 0) return string.Empty;

        bool multiple = journeys.Count > 1;
        string longUnit = unit.LongName();
        string shortUnit = unit.ShortName();
        var lines = new List<string>();

        for (int i = 0; i < journeys.Count; i++)
        {
            var journey = journeys[i];

            string date = string.IsNullOrWhiteSpace(journey.DateTag) ? "" : $" [{journey.DateTag}]";
            string heading = multiple ? $"JOURNEY {i + 1}{date}" : $"ROUTE BREAKDOWN{date}";
            string estimateNote = $"(from Google Maps; this app estimated {unit.FormatNumber(journey.EstimatedKm)} {longUnit}{(showLegDistances ? ", legs are estimates" : "")})";

            if (compact)
            {
                var legs = journey.Legs.Select(leg => showLegDistances
                    ? $"{leg.Start}  ->  {leg.End} ({unit.FormatNumber(leg.Meters / 1000.0)} {shortUnit})"
                    : $"{leg.Start}  ->  {leg.End}");
                lines.Add($"{heading}: {string.Join(", ", legs)}");
                lines.Add($"  Total: {unit.FormatNumber(journey.TotalKm)} {shortUnit}{(journey.IsVerified ? " (Google Maps)" : "")}");
                if (journey.IsVerified && showEstimate) lines.Add($"  {estimateNote}");
                if (i < journeys.Count - 1) lines.Add(string.Empty);
                continue;
            }

            lines.Add(heading);
            lines.Add(new string('─', RuleWidth));

            for (int legIndex = 0; legIndex < journey.Legs.Count; legIndex++)
            {
                var leg = journey.Legs[legIndex];
                lines.Add($"  Leg {legIndex + 1}:  {leg.Start}  ->  {leg.End}");
                if (showLegDistances)
                    lines.Add($"         Distance: {unit.FormatNumber(leg.Meters / 1000.0)} {longUnit}");
            }

            lines.Add(new string('─', RuleWidth));
            string totalTitle = multiple ? $"JOURNEY {i + 1} TOTAL" : "JOURNEY TOTAL";
            lines.Add($"  {totalTitle}: {unit.FormatNumber(journey.TotalKm)} {longUnit.ToUpperInvariant()}");
            if (journey.IsVerified)
                lines.Add(showEstimate ? $"  {estimateNote}" : "  (from Google Maps)");

            if (i < journeys.Count - 1)
            {
                lines.Add(string.Empty);
                lines.Add(new string('═', RuleWidth));
                lines.Add(string.Empty);
            }
        }

        int verified = journeys.Count(j => j.IsVerified);

        if (multiple)
        {
            double combinedKm = journeys.Sum(j => j.TotalKm);
            lines.Add(string.Empty);
            lines.Add(new string('═', RuleWidth));
            lines.Add($"  COMBINED TOTAL ({journeys.Count} JOURNEYS): {unit.FormatNumber(combinedKm)} {longUnit.ToUpperInvariant()}");

            if (verified > 0)
                lines.Add($"  ({verified} of {journeys.Count} journeys use Google Maps figures)");
        }

        if (verified > 0)
        {
            lines.Add(string.Empty);
            lines.Add($"  Note: {GoogleMapsCaveat}");
        }

        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>
    /// Formats saved history as a plain-text report for exporting: each batch with its date and breakdown,
    /// then the overall total.
    /// </summary>
    public static string FormatHistory(IReadOnlyList<JourneyBatch> batches, DistanceUnit unit, DateTime exportedAt)
    {
        var lines = new List<string> { $"POSTCODE DISTANCE CALCULATOR - SAVED HISTORY (exported {exportedAt:dd/MM/yyyy HH:mm})", string.Empty };

        foreach (var batch in batches)
        {
            string journeys = batch.Journeys.Count == 1 ? "1 journey" : $"{batch.Journeys.Count} journeys";
            lines.Add($"BATCH: {batch.CreatedAt:dd/MM/yyyy HH:mm} ({journeys})");
            lines.Add(new string('═', RuleWidth));
            lines.Add(Format(batch.Journeys, unit));
            lines.Add(string.Empty);
            lines.Add(string.Empty);
        }

        lines.Add(new string('═', RuleWidth));
        lines.Add($"  HISTORY TOTAL: {unit.FormatNumber(batches.Sum(b => b.TotalKm))} {unit.LongName().ToUpperInvariant()}");
        return string.Join(Environment.NewLine, lines);
    }
}
