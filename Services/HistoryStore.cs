using System.Text.Json;
using PostcodeDistanceCalculator.Models;

namespace PostcodeDistanceCalculator.Services;

/// <summary>
/// Saves and loads journey history as JSON. The app is portable, so by default the file lives
/// next to the executable (<see cref="DefaultPath"/>) rather than in AppData; copying the app folder
/// takes the history with it.
/// </summary>
public static class HistoryStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        // Reject nulls in non-nullable properties (e.g. "Legs": null) and missing constructor values,
        // so a damaged file is treated as unreadable instead of crashing the app later
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true
    };

    /// <summary><c>history.json</c> in the folder containing the executable.</summary>
    public static string DefaultPath => Path.Combine(AppContext.BaseDirectory, "history.json");

    /// <summary>
    /// Loads saved batches from <paramref name="path"/>. A missing file means no history yet.
    /// If the file can't be parsed or holds invalid data (nulls, missing fields) it is renamed to
    /// <c>history.corrupt-{timestamp}.json</c> (returned in <paramref name="corruptBackupPath"/>) so the
    /// next save can't overwrite the user's only copy.
    /// </summary>
    /// <exception cref="IOException">The file exists but can't be read or renamed.</exception>
    /// <exception cref="UnauthorizedAccessException">No permission to read or rename the file.</exception>
    public static List<JourneyBatch> Load(string path, out string? corruptBackupPath)
    {
        corruptBackupPath = null;
        if (!File.Exists(path)) return [];

        string json = File.ReadAllText(path);
        List<JourneyBatch>? batches;
        try
        {
            batches = JsonSerializer.Deserialize<List<JourneyBatch>>(json, JsonOptions);
        }
        catch (JsonException)
        {
            batches = null;
        }

        if (batches != null && IsValid(batches)) return batches;

        corruptBackupPath = UniqueBackupPath(path);
        File.Move(path, corruptBackupPath);
        return [];
    }

    /// <summary>
    /// Null list elements (e.g. <c>[null]</c>) aren't covered by <see cref="JsonSerializerOptions.RespectNullableAnnotations"/>,
    /// so check them explicitly.
    /// </summary>
    private static bool IsValid(List<JourneyBatch> batches)
        => batches.All(b => b != null && b.Journeys.All(j => j != null && j.Legs.All(l => l != null)));

    /// <summary><c>history.corrupt-{timestamp}.json</c> beside <paramref name="path"/>, numbered if that name is taken.</summary>
    private static string UniqueBackupPath(string path)
    {
        string folder = Path.GetDirectoryName(path) ?? ".";
        string stem = $"history.corrupt-{DateTime.Now:yyyyMMdd-HHmmss}";

        string candidate = Path.Combine(folder, stem + ".json");
        for (int n = 2; File.Exists(candidate); n++)
            candidate = Path.Combine(folder, $"{stem}-{n}.json");

        return candidate;
    }

    /// <summary>
    /// Writes <paramref name="batches"/> to <paramref name="path"/>. Writes to a temporary file first and then
    /// swaps it in, so a crash mid-write can't leave a half-written history file.
    /// </summary>
    /// <exception cref="IOException">The folder isn't writable or the disk is full.</exception>
    /// <exception cref="UnauthorizedAccessException">No permission to write, e.g. the app is in Program Files.</exception>
    public static void Save(string path, IEnumerable<JourneyBatch> batches)
    {
        string tempPath = path + ".tmp";
        File.WriteAllText(tempPath, JsonSerializer.Serialize(batches, JsonOptions));
        File.Move(tempPath, path, overwrite: true);
    }
}
