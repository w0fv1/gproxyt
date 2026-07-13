using System.IO;
using Gproxyt.Core;

namespace Gproxyt;

internal static class AppPaths
{
    public static string DataDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "gproxyt");

    public static string SettingsPath { get; } = Path.Combine(DataDirectory, "settings.json");

    public static LauncherSettingsStore CreateSettingsStore() => new(SettingsPath);
}
