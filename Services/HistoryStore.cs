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
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    /// <summary><c>history.json</c> in the folder containing the executable.</summary>
    public static string DefaultPath => Path.Combine(AppContext.BaseDirectory, "history.json");

    /// <summary>
    /// Loads saved batches from <paramref name="path"/>. A missing file means no history yet.
    /// If the file can't be parsed it is renamed to <c>history.corrupt-{timestamp}.json</c> (returned in
    /// <paramref name="corruptBackupPath"/>) so the next save can't overwrite the user's only copy.
    /// </summary>
    /// <exception cref="IOException">The file exists but can't be read or renamed.</exception>
    /// <exception cref="UnauthorizedAccessException">No permission to read or rename the file.</exception>
    public static List<JourneyBatch> Load(string path, out string? corruptBackupPath)
    {
        corruptBackupPath = null;
        if (!File.Exists(path)) return [];

        string json = File.ReadAllText(path);
        try
        {
            return JsonSerializer.Deserialize<List<JourneyBatch>>(json, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            string folder = Path.GetDirectoryName(path) ?? ".";
            corruptBackupPath = Path.Combine(folder, $"history.corrupt-{DateTime.Now:yyyyMMdd-HHmmss}.json");
            File.Move(path, corruptBackupPath);
            return [];
        }
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
