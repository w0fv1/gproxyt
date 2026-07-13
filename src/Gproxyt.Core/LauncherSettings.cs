namespace Gproxyt.Core;

public sealed record LauncherSettings(string ProxyUrl, bool RestartExisting, bool StartWithWindows)
{
    public static LauncherSettings Default { get; } = new("http://127.0.0.1:7890", true, false);

    public LauncherSettings Normalize() => this with { ProxyUrl = ProxyEndpoint.Parse(ProxyUrl).Value };
}
