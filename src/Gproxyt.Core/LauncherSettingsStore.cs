using System.Text.Json;

namespace Gproxyt.Core;

public sealed class LauncherSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly string path;

    public LauncherSettingsStore(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        this.path = Path.GetFullPath(path);
    }

    public LauncherSettings Load()
    {
        if (!File.Exists(path))
        {
            return LauncherSettings.Default;
        }

        var json = File.ReadAllText(path);
        var settings = JsonSerializer.Deserialize<LauncherSettings>(json, JsonOptions) ?? throw new InvalidDataException("设置文件内容为空。");
        return settings.Normalize();
    }

    public void Save(LauncherSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var normalized = settings.Normalize();
        var directory = Path.GetDirectoryName(path) ?? throw new InvalidOperationException("设置路径缺少目录。");
        Directory.CreateDirectory(directory);
        var temporaryPath = $"{path}.{Guid.NewGuid():N}.tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(normalized, JsonOptions));
        File.Move(temporaryPath, path, true);
    }
}
