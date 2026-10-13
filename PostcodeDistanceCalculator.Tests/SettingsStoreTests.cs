using PostcodeDistanceCalculator.Models;
using PostcodeDistanceCalculator.Services;

namespace PostcodeDistanceCalculator.Tests;

public sealed class SettingsStoreTests : IDisposable
{
    private readonly string folder = Directory.CreateTempSubdirectory("pdc-settings-tests-").FullName;
    private string SettingsPath => Path.Combine(folder, "settings.json");

    public void Dispose() => Directory.Delete(folder, recursive: true);

    [Fact]
    public void Saved_settings_load_back()
    {
        var settings = new AppSettings(CompactBreakdown: true, ShowLegDistances: false, ShowEstimate: false, Unit: DistanceUnit.Kilometers);

        SettingsStore.Save(SettingsPath, settings);

        Assert.Equal(settings, SettingsStore.Load(SettingsPath));
        Assert.Contains("\"Kilometers\"", File.ReadAllText(SettingsPath));
    }

    [Fact]
    public void Missing_or_damaged_file_gives_defaults()
    {
        Assert.Equal(new AppSettings(), SettingsStore.Load(SettingsPath));

        File.WriteAllText(SettingsPath, "not json");
        Assert.Equal(new AppSettings(), SettingsStore.Load(SettingsPath));
    }
}
