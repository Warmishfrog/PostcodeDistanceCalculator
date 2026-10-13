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

    [Theory]
    [InlineData("null")]
    [InlineData("[null]")]
    [InlineData("""[{"CreatedAt":"2026-10-13T09:30:00","Journeys":null}]""")]
    [InlineData("""[{"CreatedAt":"2026-10-13T09:30:00","Journeys":[null]}]""")]
    [InlineData("""[{"CreatedAt":"2026-10-13T09:30:00","Journeys":[{"DateTag":"","RawInput":"a","Legs":null}]}]""")]
    [InlineData("""[{"CreatedAt":"2026-10-13T09:30:00","Journeys":[{"DateTag":"","RawInput":"a"}]}]""")]
    [InlineData("""[{"CreatedAt":"2026-10-13T09:30:00","Journeys":[{"DateTag":"","RawInput":"a","Legs":[null]}]}]""")]
    public void Valid_json_with_invalid_data_is_set_aside(string json)
    {
        File.WriteAllText(HistoryPath, json);

        var history = HistoryStore.Load(HistoryPath, out string? backup);

        Assert.Empty(history);
        Assert.NotNull(backup);
        Assert.False(File.Exists(HistoryPath));
    }

    [Fact]
    public void Repeated_corrupt_files_get_separate_backups()
    {
        File.WriteAllText(HistoryPath, "first");
        HistoryStore.Load(HistoryPath, out string? firstBackup);
        File.WriteAllText(HistoryPath, "second");
        HistoryStore.Load(HistoryPath, out string? secondBackup);

        Assert.NotEqual(firstBackup, secondBackup);
        Assert.Equal("first", File.ReadAllText(firstBackup!));
        Assert.Equal("second", File.ReadAllText(secondBackup!));
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

    [Fact]
    public void Exported_text_imports_back_with_every_leg_and_google_figure()
    {
        var batch = new JourneyBatch
        {
            CreatedAt = new DateTime(2026, 10, 13, 9, 30, 0),
            Journeys = [new Journey("21st", "BS15 8DB, BS5 8NG", [new Leg("BS158DB", "BS58NG", 4321.5)]) { GoogleMapsKm = 4.6 }]
        };

        string text = HistoryStore.ToExport("REPORT FOR PEOPLE", [batch]);
        var imported = Assert.Single(HistoryStore.ParseExport(text));

        Assert.StartsWith("REPORT FOR PEOPLE", text);
        Assert.Equal(batch.CreatedAt, imported.CreatedAt);
        var journey = Assert.Single(imported.Journeys);
        Assert.Equal("21st", journey.DateTag);
        Assert.Equal(4.6, journey.GoogleMapsKm);
        Assert.Equal(new Leg("BS158DB", "BS58NG", 4321.5), Assert.Single(journey.Legs));
    }

    [Theory]
    [InlineData("Just some notes, not an export")]
    [InlineData("REPORT\n" + HistoryStore.ExportDataMarker + "\n[{ edited")]
    [InlineData("REPORT\n" + HistoryStore.ExportDataMarker + "\n[null]")]
    public void Text_that_is_not_a_valid_export_is_rejected(string text)
    {
        Assert.Throws<InvalidDataException>(() => HistoryStore.ParseExport(text));
    }
}
