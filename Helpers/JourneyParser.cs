using System.Text.RegularExpressions;

namespace PostcodeDistanceCalculator.Helpers;

/// <summary>
/// Turns the postcode input box text into routes.
/// <list type="bullet">
/// <item>Semicolons (<c>;</c>) separate journeys.</item>
/// <item>Commas or new lines separate postcodes within a journey.</item>
/// <item>
/// A line starting with a date label and a colon, e.g. <c>21st: BS15 8DB, BS5 8NG</c>, starts a new journey tagged
/// with that date, so a pasted list of dated days works. Unlabelled lines carry on the journey above them, as before.
/// </item>
/// <item>Postcodes run together without a comma (<c>BS14 0SL BS15 8DB</c>) are split apart.</item>
/// <item>Postcodes are normalised: all whitespace removed and upper-cased (culture-invariant).</item>
/// <item>A journey with fewer than two postcodes can't be routed, so it is reported in <see cref="ParseResult.Skipped"/>.</item>
/// </list>
/// </summary>
public static partial class JourneyParser
{
    private static readonly char[] PostcodeSeparators = [',', '\r', '\n'];
    private static readonly char[] LineSeparators = ['\r', '\n'];
    private const StringSplitOptions SplitOptions = StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries;

    /// <summary>A short label before a colon at the start of a line (no commas, so it can't swallow postcodes).</summary>
    [GeneratedRegex(@"^\s*(?<date>[^,:]{1,20}?)\s*:(?<rest>.*)$", RegexOptions.Singleline)]
    private static partial Regex DateLabel();

    /// <summary>One or more full UK postcodes and nothing else.</summary>
    [GeneratedRegex(@"^(\s*[A-Z]{1,2}[0-9][A-Z0-9]?\s*[0-9][A-Z]{2})+\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex PostcodeRun();

    [GeneratedRegex(@"[A-Z]{1,2}[0-9][A-Z0-9]?\s*[0-9][A-Z]{2}", RegexOptions.IgnoreCase)]
    private static partial Regex Postcode();

    /// <summary>One routable journey from the input.</summary>
    /// <param name="RawInput">The journey's original text (including any date label), kept so it can be restored into the input box.</param>
    /// <param name="Postcodes">Normalised postcodes in travel order (at least two).</param>
    /// <param name="DateTag">The journey's own date label, or null to use the Date box.</param>
    public sealed record ParsedRoute(string RawInput, IReadOnlyList<string> Postcodes, string? DateTag = null);

    /// <summary>Result of <see cref="Parse"/>.</summary>
    /// <param name="Routes">Journeys with two or more postcodes, in input order.</param>
    /// <param name="Skipped">Journeys with fewer than two postcodes, as comma-separated text for display.</param>
    public sealed record ParseResult(IReadOnlyList<ParsedRoute> Routes, IReadOnlyList<string> Skipped);

    /// <summary>Splits <paramref name="input"/> into journeys and postcodes. Never throws.</summary>
    public static ParseResult Parse(string input)
    {
        var routes = new List<ParsedRoute>();
        var skipped = new List<string>();

        foreach (string segment in input.Split(';', SplitOptions).SelectMany(SplitAtDateLabels))
        {
            string? dateTag = null;
            string body = segment;
            var label = DateLabel().Match(segment);
            if (label.Success)
            {
                dateTag = label.Groups["date"].Value;
                body = label.Groups["rest"].Value;
            }

            string[] tokens = body.Split(PostcodeSeparators, SplitOptions);
            var postcodes = tokens.SelectMany(SplitRunTogether).Select(NormalizePostcode).Where(p => p.Length > 0).ToList();

            if (postcodes.Count < 2)
                skipped.Add(dateTag == null ? string.Join(", ", tokens) : $"{dateTag}: {string.Join(", ", tokens)}");
            else
                routes.Add(new ParsedRoute(segment, postcodes, dateTag));
        }

        return new ParseResult(routes, skipped);
    }

    /// <summary>
    /// Writes routes back as tidy input text that <see cref="Parse"/> reads the same way: one journey per line with
    /// a blank line between, postcodes spaced and comma-separated, e.g. <c>21st: BS15 8DB, BS5 8NG</c>.
    /// A journey without a date label ends the line above with <c>;</c> so it doesn't join that journey.
    /// </summary>
    public static string Format(IEnumerable<ParsedRoute> routes)
    {
        var text = new System.Text.StringBuilder();
        foreach (var route in routes)
        {
            if (text.Length > 0)
                text.Append(route.DateTag == null ? ";\r\n\r\n" : "\r\n\r\n");
            text.Append(FormatRoute(route));
        }
        return text.ToString();
    }

    /// <summary>One journey as tidy input text, e.g. <c>21st: BS15 8DB, BS5 8NG</c>.</summary>
    public static string FormatRoute(ParsedRoute route)
    {
        string postcodes = string.Join(", ", route.Postcodes.Select(GoogleMapsLink.FormatPostcode));
        return route.DateTag == null ? postcodes : $"{route.DateTag}: {postcodes}";
    }

    /// <summary>
    /// Splits a semicolon-separated segment so each line with a date label starts its own journey.
    /// Unlabelled lines stay with the journey above them.
    /// </summary>
    private static IEnumerable<string> SplitAtDateLabels(string segment)
    {
        var journey = new List<string>();
        foreach (string line in segment.Split(LineSeparators, SplitOptions))
        {
            if (journey.Count > 0 && DateLabel().IsMatch(line))
            {
                yield return string.Join("\r\n", journey);
                journey.Clear();
            }
            journey.Add(line);
        }
        if (journey.Count > 0) yield return string.Join("\r\n", journey);
    }

    /// <summary>
    /// Splits a token holding several whole postcodes (a missing comma) into one per postcode.
    /// Anything else is returned unchanged so a typo is still reported as an unknown postcode.
    /// </summary>
    private static IEnumerable<string> SplitRunTogether(string token)
        => PostcodeRun().IsMatch(token) ? Postcode().Matches(token).Select(m => m.Value) : [token];

    /// <summary>
    /// Removes all whitespace (including tabs and non-breaking spaces pasted from spreadsheets)
    /// and upper-cases using the invariant culture, e.g. "sw1a 1aa" becomes "SW1A1AA".
    /// </summary>
    public static string NormalizePostcode(string postcode)
        => string.Concat(postcode.Where(c => !char.IsWhiteSpace(c))).ToUpperInvariant();
}
