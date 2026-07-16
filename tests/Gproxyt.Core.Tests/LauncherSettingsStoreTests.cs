using Gproxyt.Core;

namespace Gproxyt.Core.Tests;

public sealed class LauncherSettingsStoreTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), $"gproxyt-settings-{Guid.NewGuid():N}");

    [Fact]
    public void Load_returns_defaults_when_settings_do_not_exist()
    {
        var store = new LauncherSettingsStore(Path.Combine(directory, "settings.json"));

        Assert.Equal(LauncherSettings.Default, store.Load());
    }

    [Fact]
    public void Save_and_load_round_trip_settings()
    {
        var store = new LauncherSettingsStore(Path.Combine(directory, "settings.json"));
        var expected = new LauncherSettings("socks5://127.0.0.1:7891", false, true, "ja-JP");

        store.Save(expected);

        Assert.Equal(expected, store.Load());
    }

    [Fact]
    public void Normalize_preserves_startup_preference()
    {
        var settings = new LauncherSettings("127.0.0.1:7890", true, true);

        var normalized = settings.Normalize();

        Assert.Equal("http://127.0.0.1:7890", normalized.ProxyUrl);
        Assert.True(normalized.StartWithWindows);
    }

    [Fact]
    public void Load_preserves_legacy_settings_without_a_culture()
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "settings.json");
        File.WriteAllText(path, """{"proxyUrl":"http://127.0.0.1:7890","restartExisting":true,"startWithWindows":false}""");

        var settings = new LauncherSettingsStore(path).Load();

        Assert.Null(settings.CultureName);
    }

    public void Dispose()
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, true);
        }
    }
}
