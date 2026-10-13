using System.Globalization;
using PostcodeDistanceCalculator.Extensions;
using PostcodeDistanceCalculator.Helpers;
using PostcodeDistanceCalculator.Models;
using PostcodeDistanceCalculator.Services;

namespace PostcodeDistanceCalculator.Tests;

public sealed class GoogleMapsVerificationTests : IDisposable
{
    private readonly CultureInfo originalCulture = CultureInfo.CurrentCulture;
    private readonly string folder = Directory.CreateTempSubdirectory("pdc-verify-tests-").FullName;

    public GoogleMapsVerificationTests() => CultureInfo.CurrentCulture = new CultureInfo("en-GB");

    public void Dispose()
    {
        CultureInfo.CurrentCulture = originalCulture;
        Directory.Delete(folder, recursive: true);
    }

    private static Journey ThreeStops() => new("", "raw", [new Leg("SW1A1AA", "M11AE", 200_000), new Leg("M11AE", "EH11YZ", 100_000)]);

    [Fact]
    public void Link_goes_from_first_to_last_postcode_through_the_others()
    {
        var link = GoogleMapsLink.ForPostcodes(ThreeStops().Stops);

        Assert.NotNull(link);
        string url = link.AbsoluteUri;
        Assert.StartsWith("https://www.google.com/maps/dir/?api=1", url);
        Assert.Contains("origin=SW1A%201AA%2C%20UK", url);
        Assert.Contains("destination=EH1%201YZ%2C%20UK", url);
        Assert.Contains("waypoints=M1%201AE%2C%20UK", url);
        Assert.Contains("travelmode=driving", url);
    }

    [Fact]
    public void Two_stop_link_has_no_waypoints()
    {
        var link = GoogleMapsLink.ForPostcodes(["SW1A1AA", "M11AE"]);

        Assert.DoesNotContain("waypoints", link!.AbsoluteUri);
    }

    [Fact]
    public void No_link_when_there_are_too_many_stops()
    {
        var stops = Enumerable.Range(0, GoogleMapsLink.MaxStops + 1).Select(i => $"AB{i}1AA").ToList();

        Assert.Null(GoogleMapsLink.ForPostcodes(stops));
        Assert.NotNull(GoogleMapsLink.ForPostcodes(stops.Take(GoogleMapsLink.MaxStops).ToList()));
    }

    [Theory]
    [InlineData("SW1A1AA", "SW1A 1AA")]
    [InlineData("M11AE", "M1 1AE")]
    [InlineData("SW1A 1AA", "SW1A 1AA")]
    public void Postcodes_get_their_space_back(string normalised, string expected)
    {
        Assert.Equal(expected, GoogleMapsLink.FormatPostcode(normalised));
    }

    [Theory]
    [InlineData("210", 210)]
    [InlineData("210.4", 210.4)]
    [InlineData("210.4 mi", 210.4)]
    [InlineData(" 1,204 km", 1204)]
    [InlineData("about 15 miles", 15)]
    public void Typed_distances_are_read(string text, double expected)
    {
        Assert.True(DistanceInput.TryParse(text, out double distance));
        Assert.Equal(expected, distance, precision: 6);
    }

    [Theory]
    [InlineData("")]
    [InlineData("miles")]
    [InlineData("0")]
    public void Invalid_distances_are_rejected(string text)
    {
        Assert.False(DistanceInput.TryParse(text, out _));
    }

    [Fact]
    public void Google_figure_replaces_the_estimate_in_totals()
    {
        var journey = ThreeStops();
        var verified = journey with { GoogleMapsKm = 290 };

        Assert.False(journey.IsVerified);
        Assert.Equal(300, journey.TotalKm, precision: 6);
        Assert.True(verified.IsVerified);
        Assert.Equal(290, verified.TotalKm);
        Assert.Equal(300, verified.EstimatedKm, precision: 6);
        Assert.Equal(["SW1A1AA", "M11AE", "EH11YZ"], verified.Stops);
    }

    [Fact]
    public void Miles_typed_by_the_user_are_stored_as_km()
    {
        Assert.Equal(100, DistanceUnit.Miles.ToKm(62.1371), precision: 4);
        Assert.Equal(100, DistanceUnit.Kilometers.ToKm(100));
    }

    [Fact]
    public void Breakdown_shows_the_google_figure_and_the_estimate()
    {
        string text = BreakdownFormatter.Format([ThreeStops() with { GoogleMapsKm = 290 }], DistanceUnit.Kilometers);

        Assert.Contains("JOURNEY TOTAL: 290.0 KILOMETERS", text);
        Assert.Contains("from Google Maps; this app estimated 300.0 kilometers", text);
    }

    [Fact]
    public void Combined_breakdown_counts_verified_journeys()
    {
        string text = BreakdownFormatter.Format([ThreeStops() with { GoogleMapsKm = 290 }, ThreeStops()], DistanceUnit.Kilometers);

        Assert.Contains("COMBINED TOTAL (2 JOURNEYS): 590.0 KILOMETERS", text);
        Assert.Contains("1 of 2 journeys use Google Maps figures", text);
    }

    [Fact]
    public void Google_figures_are_saved_and_older_history_still_loads()
    {
        string path = Path.Combine(folder, "history.json");
        HistoryStore.Save(path, [new JourneyBatch { CreatedAt = DateTime.Now, Journeys = [ThreeStops() with { GoogleMapsKm = 290 }] }]);
        Assert.Equal(290, HistoryStore.Load(path, out _)[0].Journeys[0].GoogleMapsKm);

        // History saved before verification existed has no GoogleMapsKm at all
        File.WriteAllText(path, """[{"CreatedAt":"2026-10-13T09:30:00","Journeys":[{"DateTag":"","RawInput":"a","Legs":[{"Start":"A","End":"B","Meters":1000}]}]}]""");
        var journey = HistoryStore.Load(path, out string? backup)[0].Journeys[0];
        Assert.Null(backup);
        Assert.Null(journey.GoogleMapsKm);
        Assert.Equal(1, journey.TotalKm);
    }
}
