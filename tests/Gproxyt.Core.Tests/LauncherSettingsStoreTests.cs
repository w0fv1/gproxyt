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
        var expected = new LauncherSettings("socks5://127.0.0.1:7891", false, true);

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

    public void Dispose()
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, true);
        }
    }
}
