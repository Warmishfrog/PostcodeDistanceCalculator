using System.Text.Json;
using System.Text.Json.Serialization;
using PostcodeDistanceCalculator.Models;

namespace PostcodeDistanceCalculator.Services;

/// <summary>Display choices remembered between runs.</summary>
public sealed record AppSettings(
    bool CompactBreakdown = false,
    bool ShowLegDistances = true,
    bool ShowEstimate = true,
    [property: JsonConverter(typeof(JsonStringEnumConverter<DistanceUnit>))] DistanceUnit Unit = DistanceUnit.Miles);

/// <summary>
/// Saves and loads <see cref="AppSettings"/> as JSON next to the executable, like <see cref="HistoryStore"/>.
/// Settings are only conveniences, so a missing or unreadable file just gives the defaults.
/// </summary>
public static class SettingsStore
{
    /// <summary><c>settings.json</c> in the folder containing the executable.</summary>
    public static string DefaultPath => Path.Combine(AppContext.BaseDirectory, "settings.json");

    public static AppSettings Load(string path)
    {
        try
        {
            return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path)) ?? new AppSettings();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return new AppSettings();
        }
    }

    /// <summary>Writes <paramref name="settings"/>; failures are ignored, as losing a display choice isn't worth an error.</summary>
    public static void Save(string path, AppSettings settings)
    {
        try
        {
            File.WriteAllText(path, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
    }
}
