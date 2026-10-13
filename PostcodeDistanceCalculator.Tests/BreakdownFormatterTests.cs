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
    public void No_journeys_gives_empty_text()
    {
        Assert.Equal(string.Empty, BreakdownFormatter.Format([], DistanceUnit.Miles));
    }
}
