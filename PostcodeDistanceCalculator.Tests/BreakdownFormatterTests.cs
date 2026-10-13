using System.Globalization;
using PostcodeDistanceCalculator.Helpers;
using PostcodeDistanceCalculator.Models;

namespace PostcodeDistanceCalculator.Tests;

public sealed class BreakdownFormatterTests : IDisposable
{
    private readonly CultureInfo originalCulture = CultureInfo.CurrentCulture;

    private static Journey MakeJourney(string dateTag, params double[] legKm)
        => new(dateTag, "raw", legKm.Select((km, i) => new Leg($"P{i}", $"P{i + 1}", km * 1000)).ToList());

    public BreakdownFormatterTests()
    {
        // Pin decimal formatting so "12.5" doesn't become "12,5" on other locales
        CultureInfo.CurrentCulture = new CultureInfo("en-GB");
    }

    public void Dispose() => CultureInfo.CurrentCulture = originalCulture;

    [Fact]
    public void Single_journey_uses_route_breakdown_heading_and_journey_total()
    {
        string text = BreakdownFormatter.Format([MakeJourney("", 10, 2.5)], DistanceUnit.Kilometers);

        Assert.StartsWith("ROUTE BREAKDOWN", text);
        Assert.Contains("Leg 1:  P0  ->  P1", text);
        Assert.Contains("Leg 2:  P1  ->  P2", text);
        Assert.Contains("  JOURNEY TOTAL: 12.5 KILOMETERS", text);
        Assert.DoesNotContain("COMBINED", text);
    }

    [Fact]
    public void Multiple_journeys_are_numbered_with_a_combined_total()
    {
        string text = BreakdownFormatter.Format([MakeJourney("", 10), MakeJourney("", 5)], DistanceUnit.Kilometers);

        Assert.Contains("JOURNEY 1", text);
        Assert.Contains("JOURNEY 2 TOTAL: 5.0 KILOMETERS", text);
        Assert.Contains("COMBINED TOTAL (2 JOURNEYS): 15.0 KILOMETERS", text);
    }

    [Fact]
    public void Date_tag_is_shown_in_the_heading_when_present()
    {
        string text = BreakdownFormatter.Format([MakeJourney("24/09", 1)], DistanceUnit.Kilometers);

        Assert.StartsWith("ROUTE BREAKDOWN [24/09]", text);
    }

    [Fact]
    public void Distances_are_converted_to_miles()
    {
        string text = BreakdownFormatter.Format([MakeJourney("", 100)], DistanceUnit.Miles);

        Assert.Contains("JOURNEY TOTAL: 62.1 MILES", text);
    }

    [Fact]
    public void Compact_puts_each_journeys_legs_on_one_line()
    {
        string text = BreakdownFormatter.Format([MakeJourney("", 0.7, 3.4)], DistanceUnit.Kilometers, compact: true);

        Assert.StartsWith($"ROUTE BREAKDOWN:{Environment.NewLine}P0  ->  P1 (0.7 km), P1  ->  P2 (3.4 km){Environment.NewLine}", text);
        Assert.Contains("Total: 4.1 km", text);
        Assert.DoesNotContain("Leg 1", text);
    }

    [Fact]
    public void Leg_distances_can_be_hidden_in_both_layouts()
    {
        var journeys = new[] { MakeJourney("", 0.7, 3.4) };

        string full = BreakdownFormatter.Format(journeys, DistanceUnit.Kilometers, showLegDistances: false);
        string compact = BreakdownFormatter.Format(journeys, DistanceUnit.Kilometers, compact: true, showLegDistances: false);

        Assert.DoesNotContain("Distance:", full);
        Assert.Contains("JOURNEY TOTAL: 4.1 KILOMETERS", full);
        Assert.Contains("P0  ->  P1, P1  ->  P2", compact);
    }

    [Fact]
    public void Estimate_can_be_hidden_but_the_google_caveat_stays()
    {
        var verified = new[] { MakeJourney("", 10) with { GoogleMapsKm = 9 } };

        string shown = BreakdownFormatter.Format(verified, DistanceUnit.Kilometers);
        string hidden = BreakdownFormatter.Format(verified, DistanceUnit.Kilometers, showEstimate: false);

        Assert.Contains("this app estimated 10.0", shown);
        Assert.DoesNotContain("estimated", hidden);
        Assert.Contains(BreakdownFormatter.GoogleMapsCaveat, hidden);
        Assert.DoesNotContain(BreakdownFormatter.GoogleMapsCaveat, BreakdownFormatter.Format([MakeJourney("", 10)], DistanceUnit.Kilometers));
    }

    [Fact]
    public void No_journeys_gives_empty_text()
    {
        Assert.Equal(string.Empty, BreakdownFormatter.Format([], DistanceUnit.Miles));
    }

    [Fact]
    public void History_export_lists_each_batch_and_the_overall_total()
    {
        var batches = new List<JourneyBatch>
        {
            new() { CreatedAt = new DateTime(2026, 10, 1, 9, 30, 0), Journeys = [MakeJourney("21st", 10), MakeJourney("22nd", 5)] },
            new() { CreatedAt = new DateTime(2026, 10, 2, 18, 0, 0), Journeys = [MakeJourney("", 2.5)] },
        };

        string text = BreakdownFormatter.FormatHistory(batches, DistanceUnit.Kilometers, new DateTime(2026, 10, 13, 12, 0, 0));

        Assert.Contains("(exported 13/10/2026 12:00)", text);
        Assert.Contains("BATCH: 01/10/2026 09:30 (2 journeys)", text);
        Assert.Contains("JOURNEY 1 [21st]", text);
        Assert.Contains("BATCH: 02/10/2026 18:00 (1 journey)", text);
        Assert.EndsWith("HISTORY TOTAL: 17.5 KILOMETERS", text);
    }
}
