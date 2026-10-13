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

public class DatedLineTests
{
    [Fact]
    public void Each_dated_line_is_a_journey_tagged_with_its_date()
    {
        var result = JourneyParser.Parse(
            "21st: BS15 8DB, BS5 8NG, BS15 8DB\r\n" +
            "29th: BS15 8DB, BS10 5NB,, BS15 8DB \n" +
            "\n" +
            "1st: BS15 8DB, BS14 0SL BS15 8SB");

        Assert.Empty(result.Skipped);
        Assert.Equal(["21st", "29th", "1st"], result.Routes.Select(r => r.DateTag));
        Assert.Equal(["BS158DB", "BS58NG", "BS158DB"], result.Routes[0].Postcodes);
        Assert.Equal(["BS158DB", "BS105NB", "BS158DB"], result.Routes[1].Postcodes);
        Assert.Equal(["BS158DB", "BS140SL", "BS158SB"], result.Routes[2].Postcodes);
        Assert.Equal("21st: BS15 8DB, BS5 8NG, BS15 8DB", result.Routes[0].RawInput);
    }

    [Fact]
    public void Restored_batch_with_dates_joined_by_semicolons_keeps_its_dates()
    {
        var result = JourneyParser.Parse("21st: A1 1AA, B1 1BB ; C1 1CC, D1 1DD ; 22nd: E1 1EE, F1 1FF");

        Assert.Equal(["21st", null, "22nd"], result.Routes.Select(r => r.DateTag));
    }

    [Fact]
    public void Without_labels_new_lines_still_separate_postcodes()
    {
        var route = Assert.Single(JourneyParser.Parse("A1 1AA\nB1 1BB").Routes);

        Assert.Null(route.DateTag);
    }

    [Fact]
    public void Unlabelled_lines_carry_on_the_dated_journey_above()
    {
        var result = JourneyParser.Parse("A1 1AA\nB1 1BB\n21st: C1 1CC\nD1 1DD\n22nd: E1 1EE, F1 1FF");

        Assert.Equal([null, "21st", "22nd"], result.Routes.Select(r => r.DateTag));
        Assert.Equal(["A11AA", "B11BB"], result.Routes[0].Postcodes);
        Assert.Equal(["C11CC", "D11DD"], result.Routes[1].Postcodes);
        Assert.Equal("21st: C1 1CC\r\nD1 1DD", result.Routes[1].RawInput);
    }

    [Fact]
    public void Dated_line_with_one_postcode_is_skipped_with_its_date()
    {
        var result = JourneyParser.Parse("5th: A1 1AA\n6th: A1 1AA, B1 1BB");

        Assert.Equal(["5th: A1 1AA"], result.Skipped);
    }

    [Fact]
    public void Unrecognised_text_is_kept_whole_so_it_is_reported_as_unknown()
    {
        var route = Assert.Single(JourneyParser.Parse("A1 1AA, NOT A POSTCODE").Routes);

        Assert.Equal(["A11AA", "NOTAPOSTCODE"], route.Postcodes);
    }

    [Fact]
    public void Format_puts_one_tidy_journey_per_line_and_parses_back_the_same()
    {
        var parsed = JourneyParser.Parse("bs15 8db,bs5 8ng ; 21st:BS15 8DB,, bs4 3lf\nBS5 6SX\n22nd: bs15 8db, bs4 4pr");

        string text = JourneyParser.Format(parsed.Routes);

        Assert.Equal(
            "BS15 8DB, BS5 8NG\r\n\r\n" +
            "21st: BS15 8DB, BS4 3LF, BS5 6SX\r\n\r\n" +
            "22nd: BS15 8DB, BS4 4PR", text);
        var reparsed = JourneyParser.Parse(text).Routes;
        Assert.Equal(parsed.Routes.Select(r => (r.DateTag, string.Join(",", r.Postcodes))), reparsed.Select(r => (r.DateTag, string.Join(",", r.Postcodes))));
    }

    [Fact]
    public void Format_ends_the_line_with_a_semicolon_before_an_undated_journey()
    {
        var routes = new[]
        {
            new JourneyParser.ParsedRoute("", ["A11AA", "B11BB"], "21st"),
            new JourneyParser.ParsedRoute("", ["C11CC", "D11DD"]),
        };

        Assert.Equal(2, JourneyParser.Parse(JourneyParser.Format(routes)).Routes.Count);
    }
}
