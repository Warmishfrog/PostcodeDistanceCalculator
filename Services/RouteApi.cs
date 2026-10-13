using System.Globalization;
using System.Text.Json;

namespace PostcodeDistanceCalculator.Services;

/// <summary>
/// Calls the two free public APIs the app depends on:
/// <list type="bullet">
/// <item><a href="https://postcodes.io">postcodes.io</a> to turn a UK postcode into latitude/longitude.</item>
/// <item><a href="https://project-osrm.org">Project OSRM</a> to get the driving distance between two coordinates.</item>
/// </list>
/// All failures surface as exceptions with a user-readable message.
/// </summary>
public static class RouteApi
{
    /// <summary>
    /// Multiplier applied to raw OSRM distances so totals roughly match Google Maps / real odometer readings.
    /// Applied before results are stored, so saved history holds the adjusted figures.
    /// </summary>
    public const double CalibrationFactor = 1.07;

    /// <summary>OSRM's public demo server allows at most one request per second, so routing calls are spaced out.</summary>
    private static readonly RequestThrottle OsrmThrottle = new(TimeSpan.FromSeconds(1));

    private static readonly HttpClient client = new()
    {
        Timeout = TimeSpan.FromSeconds(15),
        DefaultRequestHeaders = { { "User-Agent", "WinFormsPostcodeDistanceTool" } }
    };

    /// <summary>Looks up the coordinates of a normalised UK postcode.</summary>
    public static async Task<(double Lat, double Lon)> GetCoordinatesAsync(string postcode)
    {
        string url = $"https://api.postcodes.io/postcodes/{Uri.EscapeDataString(postcode)}";

        using var response = await SendGetAsync(url, "postcodes.io");

        // 404 is the only status code indicating the postcode actually doesn't exist
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            throw new Exception($"'{postcode}' is not a recognized UK postcode.");

        // Handle other server or API endpoint failures (e.g., 500 Internal Server Error, 503 Service Unavailable, 403 Forbidden)
        if (!response.IsSuccessStatusCode)
            throw new Exception($"Postcode lookup service error (HTTP {(int)response.StatusCode} {response.ReasonPhrase}). Please check the API connection.");

        try
        {
            using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);
            var root = doc.RootElement;

            if (!root.TryGetProperty("result", out var result) || result.ValueKind == JsonValueKind.Null)
                throw new Exception($"'{postcode}' returned no coordinate data.");

            // Some valid postcodes (non-geographic, PO boxes, Channel Islands) come back with null coordinates
            if (!result.TryGetProperty("latitude", out var lat) || lat.ValueKind != JsonValueKind.Number ||
                !result.TryGetProperty("longitude", out var lon) || lon.ValueKind != JsonValueKind.Number)
                throw new Exception($"'{postcode}' is a valid postcode but has no map location (e.g. a PO box or non-geographic postcode), so it can't be routed.");

            return (lat.GetDouble(), lon.GetDouble());
        }
        catch (JsonException ex)
        {
            throw new Exception("Failed to parse response from postcodes.io API.", ex);
        }
    }

    /// <summary>Raw (uncalibrated) driving distance in metres between two coordinates.</summary>
    public static async Task<double> GetDrivingDistanceMetersAsync((double Lat, double Lon) start, (double Lat, double Lon) end)
    {
        // CultureInfo.InvariantCulture prevents comma decimal separators (e.g. 51,500 instead of 51.500)
        string startLon = start.Lon.ToString(CultureInfo.InvariantCulture);
        string startLat = start.Lat.ToString(CultureInfo.InvariantCulture);
        string endLon = end.Lon.ToString(CultureInfo.InvariantCulture);
        string endLat = end.Lat.ToString(CultureInfo.InvariantCulture);

        string url = $"http://router.project-osrm.org/route/v1/driving/{startLon},{startLat};{endLon},{endLat}?overview=false";

        await OsrmThrottle.WaitAsync();
        using var response = await SendGetAsync(url, "the routing service (OSRM)");

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Routing service error (HTTP {(int)response.StatusCode} {response.ReasonPhrase}). Please try again later.");

        try
        {
            using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);
            var root = doc.RootElement;

            // OSRM returns a "code" field (e.g. "Ok", "NoRoute", "InvalidQuery")
            if (root.TryGetProperty("code", out var codeElement))
            {
                string code = codeElement.GetString() ?? string.Empty;
                if (code == "NoRoute")
                    throw new Exception("No drivable route could be found between these coordinates.");

                if (code != "Ok")
                    throw new Exception($"Routing service returned error code: '{code}'.");
            }

            if (!root.TryGetProperty("routes", out var routes) || routes.ValueKind != JsonValueKind.Array || routes.GetArrayLength() == 0)
                throw new Exception("No drivable route found for the given postcodes.");

            var firstRoute = routes[0];
            if (!firstRoute.TryGetProperty("distance", out var distanceElement))
                throw new Exception("Route data received, but distance property was missing.");

            return distanceElement.GetDouble();
        }
        catch (JsonException ex)
        {
            throw new Exception("Failed to parse response from OSRM routing service.", ex);
        }
    }

    private static async Task<HttpResponseMessage> SendGetAsync(string url, string serviceName)
    {
        try
        {
            return await client.GetAsync(url);
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Network error reaching {serviceName}: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new Exception($"The request to {serviceName} timed out.", ex);
        }
    }
}
