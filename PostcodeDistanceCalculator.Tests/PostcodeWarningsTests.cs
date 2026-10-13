using PostcodeDistanceCalculator.Helpers;

namespace PostcodeDistanceCalculator.Tests;

public class PostcodeWarningsTests
{
    // Shaped like a month of trips from home: BS158DB used a lot, BS140SL a few times, BS140SU once
    private static readonly Dictionary<string, int> Usage = PostcodeWarnings.CountUsage(
        [.. Enumerable.Repeat("BS158DB", 20), "BS140SL", "BS140SL", "BS140SL", "BS140SU", "BS158SB", "BS58NG"]);

    [Fact]
    public void Neighbouring_key_typo_of_a_common_postcode_is_flagged()
    {
        // S is next to D on the keyboard: the real typo from a pasted month of trips
        string? warning = PostcodeWarnings.Check("BS158SB", Usage);

        Assert.NotNull(warning);
        Assert.Contains("BS15 8DB", warning);
    }

    [Fact]
    public void Real_postcode_one_letter_from_another_is_not_flagged_when_the_keys_are_far_apart()
    {
        // L and U aren't neighbours, so BS14 0SU next to the more common BS14 0SL is left alone
        Assert.Null(PostcodeWarnings.Check("BS140SU", Usage));
    }

    [Fact]
    public void Common_postcodes_are_not_flagged()
    {
        Assert.Null(PostcodeWarnings.Check("BS158DB", Usage));
        Assert.Null(PostcodeWarnings.Check("BS58NG", Usage));
    }

    [Theory]
    [InlineData("BS158D")]
    [InlineData("NOTAPOSTCODE")]
    [InlineData("1BS 5DB")]
    public void Text_not_in_postcode_format_is_flagged(string postcode)
    {
        Assert.Contains("Doesn't look like a UK postcode", PostcodeWarnings.Check(postcode, Usage));
    }

    [Fact]
    public void Unit_letters_that_are_never_used_are_flagged()
    {
        Assert.Contains("never end with", PostcodeWarnings.Check("BS158DC", Usage));
    }

    [Theory]
    [InlineData("BS158DB", "BS158SB", true)]  // neighbouring keys
    [InlineData("BS158DB", "BS158BD", true)]  // swapped round
    [InlineData("SW1A1AA", "SW1A1A4", false)] // A and 4 aren't neighbours
    [InlineData("BS10AA", "BS1OAA", true)]    // 0 and O look alike
    [InlineData("BS158DB", "BS158DBX", false)]
    public void One_slip_detection(string a, string b, bool expected)
    {
        Assert.Equal(expected, PostcodeWarnings.IsOneSlipAway(a, b));
    }

    [Fact]
    public void Finds_postcodes_in_the_input_with_positions_skipping_date_labels()
    {
        const string text = "21st: BS15 8DB, bs5 8ng\r\n1st: BS14 0SL BS15 8SB,, oops";

        var spans = PostcodeWarnings.FindInInput(text);

        Assert.Equal(["BS158DB", "BS58NG", "BS140SL", "BS158SB", "OOPS"], spans.Select(s => s.Postcode));
        Assert.All(spans, s => Assert.Equal(s.Postcode, JourneyParser.NormalizePostcode(text.Substring(s.Start, s.Length))));
    }
}
