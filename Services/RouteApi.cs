using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using PostcodeDistanceCalculator.Models;

namespace PostcodeDistanceCalculator.Services;

/// <summary>
/// Calls the two free public APIs the app depends on:
/// <list type="bullet">
/// <item><a href="https://postcodes.io">postcodes.io</a> to turn UK postcodes into latitude/longitude, all in one bulk request.</item>
/// <item><a href="https://project-osrm.org">Project OSRM</a> to get driving distances, one request per journey covering every leg.</item>
/// </list>
/// Expected failures throw <see cref="RouteLookupException"/> with a user-readable message.
/// </summary>
public static class RouteApi
{
    /// <summary>
    /// Multiplier applied to raw OSRM distances so totals roughly match Google Maps / real odometer readings.
    /// Applied before results are stored, so saved history holds the adjusted figures.
    /// </summary>
    public const double CalibrationFactor = 1.07;

    /// <summary>postcodes.io accepts at most 100 postcodes per bulk lookup.</summary>
    private const int MaxPostcodesPerLookup = 100;

    /// <summary>OSRM's public demo server allows at most one request per second, so routing calls are spaced out.</summary>
    private static readonly RequestThrottle OsrmThrottle = new(TimeSpan.FromSeconds(1));

    private static readonly HttpClient client = new()
    {
        Timeout = TimeSpan.FromSeconds(15),
        DefaultRequestHeaders = { { "User-Agent", "WinFormsPostcodeDistanceTool" } }
    };

    /// <summary>
    /// Looks up the location of every normalised postcode in <paramref name="postcodes"/> (bulk requests of up to 100).
    /// Postcodes that are no longer in use fall back to their last known location (<see cref="PostcodeLocation.YearTerminated"/> is set).
    /// </summary>
    /// <exception cref="RouteLookupException">Any postcode is unknown or has no map location, or a service fails.</exception>
    public static async Task<IReadOnlyDictionary<string, PostcodeLocation>> GetLocationsAsync(IEnumerable<string> postcodes)
    {
        var locations = new Dictionary<string, PostcodeLocation>();
        var noLocation = new List<string>();
        var notFound = new List<string>();

        foreach (string[] chunk in postcodes.Distinct().Chunk(MaxPostcodesPerLookup))
        {
            string json = (await SendAsync(
                () => client.PostAsJsonAsync("https://api.postcodes.io/postcodes", new { postcodes = chunk }), "postcodes.io"))!;

            foreach (var (postcode, exists, location) in ParseBulkLookup(json))
                Sort(postcode, exists, location);
        }

        // Not in the current list: check them all against retired postcodes at once (they still have a last known location)
        var candidates = notFound.ToList();
        notFound.Clear();
        var retired = await Task.WhenAll(candidates.Select(async postcode => (Postcode: postcode, Result: await GetTerminatedLocationAsync(postcode))));
        foreach (var (postcode, (exists, location)) in retired)
            Sort(postcode, exists, location);

        var problems = new List<string>();
        if (notFound.Count > 0)
            problems.Add($"Not recognised as UK postcodes: {string.Join(", ", notFound)}.");
        if (noLocation.Count > 0)
            problems.Add($"Valid but with no map location (e.g. PO boxes or non-geographic postcodes), so they can't be routed: {string.Join(", ", noLocation)}.");
        if (problems.Count > 0)
            throw new RouteLookupException(string.Join("\n", problems));

        return locations;

        void Sort(string postcode, bool exists, PostcodeLocation? location)
        {
            if (location != null) locations[postcode] = location;
            else if (exists) noLocation.Add(postcode);
            else notFound.Add(postcode);
        }
    }

    /// <summary>
    /// Raw (uncalibrated) driving distance in metres for each leg of a route through <paramref name="stops"/> in order,
    /// using a single OSRM request. Returns <c>stops.Count - 1</c> distances.
    /// </summary>
    /// <exception cref="RouteLookupException">No route exists, or the routing service fails.</exception>
    public static async Task<IReadOnlyList<double>> GetLegDistancesMetersAsync(IReadOnlyList<PostcodeLocation> stops)
    {
        // CultureInfo.InvariantCulture prevents comma decimal separators (e.g. 51,500 instead of 51.500)
        string coordinates = string.Join(";", stops.Select(s =>
            $"{s.Longitude.ToString(CultureInfo.InvariantCulture)},{s.Latitude.ToString(CultureInfo.InvariantCulture)}"));

        await OsrmThrottle.WaitAsync();
        string json = (await SendAsync(
            () => client.GetAsync($"https://router.project-osrm.org/route/v1/driving/{coordinates}?overview=false"),
            "the routing service (OSRM)",
            // OSRM reports problems like NoRoute in the JSON body with a 400 status, so let ParseRoute explain them
            acceptBadRequest: true))!;

        return ParseRoute(json, stops.Count - 1);
    }

    /// <summary>
    /// Looks <paramref name="postcode"/> up in postcodes.io's list of retired postcodes. <c>Exists</c> is false if it was never
    /// a postcode; <c>Location</c> is null if it existed but has no map location.
    /// </summary>
    private static async Task<(bool Exists, PostcodeLocation? Location)> GetTerminatedLocationAsync(string postcode)
    {
        string url = $"https://api.postcodes.io/terminated_postcodes/{Uri.EscapeDataString(postcode)}";
        string? json = await SendAsync(() => client.GetAsync(url), "postcodes.io", notFoundAsNull: true);
        if (json == null) return (false, null);

        try
        {
            using var doc = JsonDocument.Parse(json);
            var result = doc.RootElement.GetProperty("result");
            if (!TryReadLocation(result, out double lat, out double lon)) return (true, null);

            int? yearTerminated = result.TryGetProperty("year_terminated", out var year) && year.ValueKind == JsonValueKind.Number ? year.GetInt32() : null;
            return (true, new PostcodeLocation(lat, lon, IsTerminated: true, yearTerminated));
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            throw new RouteLookupException("Failed to read the response from postcodes.io.", ex);
        }
    }

    /// <summary>
    /// Reads a postcodes.io bulk lookup response: one entry per queried postcode. <c>Exists</c> is false when it isn't a
    /// current postcode; <c>Location</c> is null when it isn't found or has no map location (e.g. PO boxes).
    /// </summary>
    internal static IReadOnlyList<(string Postcode, bool Exists, PostcodeLocation? Location)> ParseBulkLookup(string json)
    {
        var results = new List<(string, bool, PostcodeLocation?)>();
        try
        {
            using var doc = JsonDocument.Parse(json);
            foreach (var item in doc.RootElement.GetProperty("result").EnumerateArray())
            {
                string postcode = item.GetProperty("query").GetString() ?? string.Empty;
                var result = item.GetProperty("result");

                if (result.ValueKind == JsonValueKind.Null)
                    results.Add((postcode, false, null));
                else if (TryReadLocation(result, out double lat, out double lon))
                    results.Add((postcode, true, new PostcodeLocation(lat, lon)));
                else
                    results.Add((postcode, true, null));
            }
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            throw new RouteLookupException("Failed to read the response from postcodes.io.", ex);
        }

        return results;
    }

    /// <summary>Reads per-leg distances (metres) from an OSRM route response, checking there's one per expected leg.</summary>
    internal static IReadOnlyList<double> ParseRoute(string json, int expectedLegs)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // OSRM returns a "code" field (e.g. "Ok", "NoRoute", "InvalidQuery")
            string code = root.TryGetProperty("code", out var codeElement) ? codeElement.GetString() ?? "" : "";
            if (code == "NoRoute")
                throw new RouteLookupException("No drivable route could be found between these postcodes.");
            if (code != "Ok")
            {
                // OSRM explains errors like InvalidQuery or TooBig in "message"
                string? message = root.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String ? m.GetString() : null;
                throw new RouteLookupException(message is null
                    ? $"The routing service returned error code '{code}'."
                    : $"The routing service returned error code '{code}': {message}");
            }

            var legs = root.GetProperty("routes")[0].GetProperty("legs");
            var distances = legs.EnumerateArray().Select(leg => leg.GetProperty("distance").GetDouble()).ToList();

            if (distances.Count != expectedLegs)
                throw new RouteLookupException($"The routing service returned {distances.Count} legs, expected {expectedLegs}.");

            return distances;
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException or IndexOutOfRangeException)
        {
            throw new RouteLookupException("Failed to read the response from the routing service (OSRM).", ex);
        }
    }

    private static bool TryReadLocation(JsonElement result, out double latitude, out double longitude)
    {
        latitude = longitude = 0;
        if (!result.TryGetProperty("latitude", out var lat) || lat.ValueKind != JsonValueKind.Number ||
            !result.TryGetProperty("longitude", out var lon) || lon.ValueKind != JsonValueKind.Number)
            return false;

        latitude = lat.GetDouble();
        longitude = lon.GetDouble();
        return true;
    }

    /// <summary>
    /// Sends a request and returns the response body, turning network errors, timeouts and HTTP errors into
    /// <see cref="RouteLookupException"/>. With <paramref name="notFoundAsNull"/>, a 404 returns null instead;
    /// otherwise the result is never null.
    /// </summary>
    private static async Task<string?> SendAsync(Func<Task<HttpResponseMessage>> send, string serviceName, bool notFoundAsNull = false, bool acceptBadRequest = false)
    {
        HttpResponseMessage response;
        try
        {
            response = await send();
        }
        catch (HttpRequestException ex)
        {
            throw new RouteLookupException($"Network error reaching {serviceName}: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new RouteLookupException($"The request to {serviceName} timed out.", ex);
        }

        using (response)
        {
            if (notFoundAsNull && response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            if (!response.IsSuccessStatusCode && !(acceptBadRequest && response.StatusCode == System.Net.HttpStatusCode.BadRequest))
                throw new RouteLookupException($"{serviceName} error (HTTP {(int)response.StatusCode} {response.ReasonPhrase}). Please try again later.");

            return await response.Content.ReadAsStringAsync();
        }
    }
}
