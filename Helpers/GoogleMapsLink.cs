namespace PostcodeDistanceCalculator.Helpers;

/// <summary>
/// Builds <a href="https://developers.google.com/maps/documentation/urls/get-started">Google Maps URLs</a> that open
/// driving directions through a journey's postcodes, so the user can check the app's figures against Google's.
/// Maps URLs need no API key and are free to use.
/// </summary>
public static class GoogleMapsLink
{
    /// <summary>Google Maps on a desktop browser supports up to 9 stops between the start and the end.</summary>
    public const int MaxStops = 11;

    /// <summary>
    /// Driving directions from the first postcode to the last, through the others in order.
    /// Returns null when there are fewer than 2 or more than <see cref="MaxStops"/> postcodes.
    /// </summary>
    public static Uri? ForPostcodes(IReadOnlyList<string> postcodes)
    {
        if (postcodes.Count < 2 || postcodes.Count > MaxStops) return null;

        string Param(string postcode) => Uri.EscapeDataString(FormatPostcode(postcode) + ", UK");

        string url = "https://www.google.com/maps/dir/?api=1" +
            $"&origin={Param(postcodes[0])}" +
            $"&destination={Param(postcodes[^1])}" +
            "&travelmode=driving";

        if (postcodes.Count > 2)
            url += "&waypoints=" + string.Join("%7C", postcodes.Skip(1).Take(postcodes.Count - 2).Select(Param));

        return new Uri(url);
    }

    /// <summary>
    /// Puts back the space before the 3-character inward code that normalisation removed,
    /// e.g. "SW1A1AA" becomes "SW1A 1AA", so Google recognises it as a postcode.
    /// </summary>
    public static string FormatPostcode(string postcode)
        => postcode.Length > 3 && !postcode.Contains(' ') ? $"{postcode[..^3]} {postcode[^3..]}" : postcode;
}
