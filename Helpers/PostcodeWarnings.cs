using System.Text.RegularExpressions;

namespace PostcodeDistanceCalculator.Helpers;

/// <summary>
/// Spots postcodes that look wrong, so the user can check them for typos. These are only warnings: a flagged
/// postcode still calculates if postcodes.io knows it. A typo that happens to be another real postcode
/// (e.g. <c>BS15 8SB</c> for <c>BS15 8DB</c>) can't be caught by the lookup, which is what the typo check is for.
/// <list type="bullet">
/// <item>Not in the UK postcode format, or ending in letters UK postcodes never use (C, I, K, M, O, V).</item>
/// <item>
/// One slip away from a postcode used at least twice as often (across the input and history): one character
/// swapped for a neighbouring key or a look-alike (S/D, O/0), or two neighbouring characters swapped round.
/// </item>
/// </list>
/// </summary>
public static partial class PostcodeWarnings
{
    /// <summary>A text range in the input box holding one postcode (or something that should have been one).</summary>
    /// <param name="Start">Index of the first character.</param>
    /// <param name="Length">Number of characters.</param>
    /// <param name="Postcode">The text normalised with <see cref="JourneyParser.NormalizePostcode"/>.</param>
    public sealed record PostcodeSpan(int Start, int Length, string Postcode);

    [GeneratedRegex("^[A-Z]{1,2}[0-9][A-Z0-9]?[0-9][A-Z]{2}$")]
    private static partial Regex PostcodeFormat();

    /// <summary>The last two letters (the unit) never use these.</summary>
    private const string UnusedUnitLetters = "CIKMOV";

    [GeneratedRegex(@"[A-Z]{1,2}[0-9][A-Z0-9]?\s*[0-9][A-Z]{2}", RegexOptions.IgnoreCase)]
    private static partial Regex PostcodeInText();

    [GeneratedRegex(@"^(\s*[A-Z]{1,2}[0-9][A-Z0-9]?\s*[0-9][A-Z]{2})+\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex PostcodeRun();

    /// <summary>Text between commas, semicolons and line breaks.</summary>
    [GeneratedRegex(@"[^,;\r\n]+")]
    private static partial Regex Token();

    private static readonly string[] KeyboardRows = ["1234567890", "QWERTYUIOP", "ASDFGHJKL", "ZXCVBNM"];
    private static readonly string[] LookAlikes = ["O0", "I1", "L1", "S5", "B8", "Z2", "G6"];

    /// <summary>How often each normalised postcode appears in <paramref name="postcodes"/>.</summary>
    public static Dictionary<string, int> CountUsage(IEnumerable<string> postcodes)
        => postcodes.GroupBy(p => p).ToDictionary(g => g.Key, g => g.Count());

    /// <summary>
    /// The warning for one normalised <paramref name="postcode"/>, or null if it looks fine.
    /// <paramref name="usage"/> is how often each postcode is used, from <see cref="CountUsage"/>.
    /// </summary>
    public static string? Check(string postcode, IReadOnlyDictionary<string, int> usage)
    {
        if (!PostcodeFormat().IsMatch(postcode))
            return "Doesn't look like a UK postcode (e.g. BS15 8DB).";

        if (postcode[^2..].Any(c => UnusedUnitLetters.Contains(c)))
            return $"UK postcodes never end with {string.Join(", ", UnusedUnitLetters.ToCharArray())}, so this may be a typo.";

        int count = usage.GetValueOrDefault(postcode);
        var likely = usage
            .Where(u => u.Key != postcode && u.Value >= 2 && u.Value >= 2 * count && IsOneSlipAway(postcode, u.Key))
            .OrderByDescending(u => u.Value)
            .Select(u => u.Key)
            .FirstOrDefault();

        return likely == null ? null : $"Possible typo: did you mean {GoogleMapsLink.FormatPostcode(likely)}? It's used more often.";
    }

    /// <summary>
    /// Every postcode in the input box text with its position, for underlining. Date labels (<c>21st:</c>) are
    /// skipped, postcodes run together without a comma are found separately, and anything else between separators
    /// is returned whole so it can be flagged as not looking like a postcode.
    /// </summary>
    public static IReadOnlyList<PostcodeSpan> FindInInput(string text)
    {
        var spans = new List<PostcodeSpan>();
        foreach (Match token in Token().Matches(text))
        {
            // Skip a date label: everything up to the colon
            int start = token.Index;
            int colon = token.Value.IndexOf(':');
            if (colon >= 0) start += colon + 1;

            string body = text[start..(token.Index + token.Length)];
            int trimmedStart = start + (body.Length - body.TrimStart().Length);
            string trimmed = body.Trim();
            if (trimmed.Length == 0) continue;

            if (PostcodeRun().IsMatch(trimmed))
            {
                foreach (Match postcode in PostcodeInText().Matches(trimmed))
                    spans.Add(new PostcodeSpan(trimmedStart + postcode.Index, postcode.Length, JourneyParser.NormalizePostcode(postcode.Value)));
            }
            else
            {
                spans.Add(new PostcodeSpan(trimmedStart, trimmed.Length, JourneyParser.NormalizePostcode(trimmed)));
            }
        }
        return spans;
    }

    /// <summary>
    /// True when <paramref name="a"/> and <paramref name="b"/> differ by one character that is a neighbouring key
    /// or a look-alike, or by two neighbouring characters swapped round.
    /// </summary>
    internal static bool IsOneSlipAway(string a, string b)
    {
        if (a.Length != b.Length) return false;

        var differences = Enumerable.Range(0, a.Length).Where(i => a[i] != b[i]).ToList();
        return differences switch
        {
            [int i] => AreNeighbours(a[i], b[i]) || LookAlikes.Any(pair => pair.Contains(a[i]) && pair.Contains(b[i])),
            [int i, int j] => j == i + 1 && a[i] == b[j] && a[j] == b[i],
            _ => false
        };
    }

    /// <summary>True when the two keys touch on a UK/US QWERTY keyboard (same row, or the staggered row above or below).</summary>
    private static bool AreNeighbours(char a, char b)
    {
        (int Row, int Column)? Find(char c)
        {
            for (int row = 0; row < KeyboardRows.Length; row++)
            {
                int column = KeyboardRows[row].IndexOf(c);
                if (column >= 0) return (row, column);
            }
            return null;
        }

        if (Find(a) is not var (rowA, columnA) || Find(b) is not var (rowB, columnB)) return false;

        // Each row is shifted right by about half a key, so the key below column c is c-1 or c
        return (rowB - rowA) switch
        {
            0 => Math.Abs(columnA - columnB) == 1,
            1 => columnB == columnA || columnB == columnA - 1,
            -1 => columnB == columnA || columnB == columnA + 1,
            _ => false
        };
    }
}
