using PostcodeDistanceCalculator.Models;
using PostcodeDistanceCalculator.Services;

namespace PostcodeDistanceCalculator.Tests;

public sealed class HistoryStoreTests : IDisposable
{
    private readonly string folder = Directory.CreateTempSubdirectory("pdc-history-tests-").FullName;
    private string HistoryPath => Path.Combine(folder, "history.json");

    public void Dispose() => Directory.Delete(folder, recursive: true);

    [Fact]
    public void Missing_file_loads_as_empty_history()
    {
        var history = HistoryStore.Load(HistoryPath, out string? backup);

        Assert.Empty(history);
        Assert.Null(backup);
    }

    [Fact]
    public void Saved_batches_load_back_identically()
    {
        var createdAt = new DateTime(2026, 10, 13, 9, 30, 0);
        var batch = new JourneyBatch
        {
            CreatedAt = createdAt,
            Journeys = [new Journey("13/10", "SW1A 1AA, M1 1AE", [new Leg("SW1A1AA", "M11AE", 331450.2)])]
        };

        HistoryStore.Save(HistoryPath, [batch]);
        var loaded = Assert.Single(HistoryStore.Load(HistoryPath, out _));

        Assert.Equal(createdAt, loaded.CreatedAt);
        var journey = Assert.Single(loaded.Journeys);
        Assert.Equal("13/10", journey.DateTag);
        Assert.Equal("SW1A 1AA, M1 1AE", journey.RawInput);
        Assert.Equal(new Leg("SW1A1AA", "M11AE", 331450.2), Assert.Single(journey.Legs));
        Assert.False(File.Exists(HistoryPath + ".tmp"));
    }

    [Fact]
    public void Saving_replaces_previous_contents()
    {
        HistoryStore.Save(HistoryPath, [new JourneyBatch { CreatedAt = DateTime.Now }]);
        HistoryStore.Save(HistoryPath, []);

        Assert.Empty(HistoryStore.Load(HistoryPath, out _));
    }

    [Fact]
    public void Corrupt_file_is_set_aside_instead_of_being_overwritten()
    {
        File.WriteAllText(HistoryPath, "{ not valid json");

        var history = HistoryStore.Load(HistoryPath, out string? backup);

        Assert.Empty(history);
        Assert.NotNull(backup);
        Assert.False(File.Exists(HistoryPath));
        Assert.Equal("{ not valid json", File.ReadAllText(backup));
    }
}
