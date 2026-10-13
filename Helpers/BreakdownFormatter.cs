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

    /// <summary>Formats <paramref name="journeys"/> in <paramref name="unit"/>. Returns an empty string for no journeys.</summary>
    public static string Format(IReadOnlyList<Journey> journeys, DistanceUnit unit)
    {
        if (journeys.Count == 0) return string.Empty;

        bool multiple = journeys.Count > 1;
        string longUnit = unit.LongName();
        var lines = new List<string>();

        for (int i = 0; i < journeys.Count; i++)
        {
            var journey = journeys[i];

            string date = string.IsNullOrWhiteSpace(journey.DateTag) ? "" : $" [{journey.DateTag}]";
            lines.Add(multiple ? $"JOURNEY {i + 1}{date}" : $"ROUTE BREAKDOWN{date}");
            lines.Add(new string('─', RuleWidth));

            for (int legIndex = 0; legIndex < journey.Legs.Count; legIndex++)
            {
                var leg = journey.Legs[legIndex];
                lines.Add($"  Leg {legIndex + 1}:  {leg.Start}  ->  {leg.End}");
                lines.Add($"         Distance: {unit.FormatNumber(leg.Meters / 1000.0)} {longUnit}");
            }

            lines.Add(new string('─', RuleWidth));
            string totalTitle = multiple ? $"JOURNEY {i + 1} TOTAL" : "JOURNEY TOTAL";
            lines.Add($"  {totalTitle}: {unit.FormatNumber(journey.TotalKm)} {longUnit.ToUpperInvariant()}");
            if (journey.IsVerified)
                lines.Add($"  (from Google Maps; this app estimated {unit.FormatNumber(journey.EstimatedKm)} {longUnit}, legs above are estimates)");

            if (i < journeys.Count - 1)
            {
                lines.Add(string.Empty);
                lines.Add(new string('═', RuleWidth));
                lines.Add(string.Empty);
            }
        }

        if (multiple)
        {
            double combinedKm = journeys.Sum(j => j.TotalKm);
            lines.Add(string.Empty);
            lines.Add(new string('═', RuleWidth));
            lines.Add($"  COMBINED TOTAL ({journeys.Count} JOURNEYS): {unit.FormatNumber(combinedKm)} {longUnit.ToUpperInvariant()}");

            int verified = journeys.Count(j => j.IsVerified);
            if (verified > 0)
                lines.Add($"  ({verified} of {journeys.Count} journeys use Google Maps figures)");
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
