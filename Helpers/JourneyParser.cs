namespace PostcodeDistanceCalculator.Helpers;

/// <summary>
/// Turns the postcode input box text into routes.
/// <list type="bullet">
/// <item>Semicolons (<c>;</c>) separate journeys.</item>
/// <item>Commas or new lines separate postcodes within a journey.</item>
/// <item>Postcodes are normalised: all whitespace removed and upper-cased (culture-invariant).</item>
/// <item>A journey with fewer than two postcodes can't be routed, so it is reported in <see cref="ParseResult.Skipped"/>.</item>
/// </list>
/// </summary>
public static class JourneyParser
{
    private static readonly char[] PostcodeSeparators = [',', '\r', '\n'];
    private const StringSplitOptions SplitOptions = StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries;

    /// <summary>One routable journey from the input.</summary>
    /// <param name="RawInput">The journey's original text, kept so it can be restored into the input box.</param>
    /// <param name="Postcodes">Normalised postcodes in travel order (at least two).</param>
    public sealed record ParsedRoute(string RawInput, IReadOnlyList<string> Postcodes);

    /// <summary>Result of <see cref="Parse"/>.</summary>
    /// <param name="Routes">Journeys with two or more postcodes, in input order.</param>
    /// <param name="Skipped">Journeys with fewer than two postcodes, as comma-separated text for display.</param>
    public sealed record ParseResult(IReadOnlyList<ParsedRoute> Routes, IReadOnlyList<string> Skipped);

    /// <summary>Splits <paramref name="input"/> into journeys and postcodes. Never throws.</summary>
    public static ParseResult Parse(string input)
    {
        var routes = new List<ParsedRoute>();
        var skipped = new List<string>();

        foreach (string segment in input.Split(';', SplitOptions))
        {
            string[] tokens = segment.Split(PostcodeSeparators, SplitOptions);
            var postcodes = tokens.Select(NormalizePostcode).Where(p => p.Length > 0).ToList();

            if (postcodes.Count < 2)
                skipped.Add(string.Join(", ", tokens));
            else
                routes.Add(new ParsedRoute(segment, postcodes));
        }

        return new ParseResult(routes, skipped);
    }

    /// <summary>
    /// Removes all whitespace (including tabs and non-breaking spaces pasted from spreadsheets)
    /// and upper-cases using the invariant culture, e.g. "sw1a 1aa" becomes "SW1A1AA".
    /// </summary>
    public static string NormalizePostcode(string postcode)
        => string.Concat(postcode.Where(c => !char.IsWhiteSpace(c))).ToUpperInvariant();
}
