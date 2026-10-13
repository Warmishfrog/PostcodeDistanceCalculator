using System.Globalization;
using PostcodeDistanceCalculator.Helpers;

namespace PostcodeDistanceCalculator.Tests;

public class JourneyParserTests
{
    [Fact]
    public void Commas_separate_postcodes_and_semicolons_separate_journeys()
    {
        var result = JourneyParser.Parse("SW1A 1AA, M1 1AE ; B1 1AA, LS1 1UR, CB2 1TN");

        Assert.Equal(2, result.Routes.Count);
        Assert.Equal(["SW1A1AA", "M11AE"], result.Routes[0].Postcodes);
        Assert.Equal(["B11AA", "LS11UR", "CB21TN"], result.Routes[1].Postcodes);
        Assert.Empty(result.Skipped);
    }

    [Fact]
    public void New_lines_separate_postcodes()
    {
        var result = JourneyParser.Parse("SW1A 1AA\r\nM1 1AE\nEH1 1YZ");

        Assert.Equal(["SW1A1AA", "M11AE", "EH11YZ"], Assert.Single(result.Routes).Postcodes);
    }

    [Fact]
    public void All_whitespace_is_stripped_and_postcodes_are_upper_cased()
    {
        // Tab and non-breaking space, as pasted from a spreadsheet
        var result = JourneyParser.Parse("sw1a\t1aa, m1 1ae");

        Assert.Equal(["SW1A1AA", "M11AE"], Assert.Single(result.Routes).Postcodes);
    }

    [Fact]
    public void Upper_casing_ignores_the_current_culture()
    {
        // In Turkish, "i".ToUpper() is a dotted capital I, which would break the postcode
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
            Assert.Equal("IG11AA", JourneyParser.NormalizePostcode("ig1 1aa"));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void Journeys_with_fewer_than_two_postcodes_are_reported_as_skipped()
    {
        var result = JourneyParser.Parse("A1 1AA, B1 1BB ; C1 1CC ; D1 1DD\r\n ; E1 1EE, F1 1FF");

        Assert.Equal(2, result.Routes.Count);
        Assert.Equal(["C1 1CC", "D1 1DD"], result.Skipped);
    }

    [Fact]
    public void Raw_input_is_kept_per_journey_for_restoring_the_input_box()
    {
        var result = JourneyParser.Parse(" a1 1aa, b1 1bb ;c1 1cc,d1 1dd");

        Assert.Equal("a1 1aa, b1 1bb", result.Routes[0].RawInput);
        Assert.Equal("c1 1cc,d1 1dd", result.Routes[1].RawInput);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ; ; ")]
    [InlineData(",,,")]
    public void Empty_input_gives_no_routes(string input)
    {
        Assert.Empty(JourneyParser.Parse(input).Routes);
    }
}
