using System.IO;

namespace Gproxyt;

internal sealed record ApplicationOptions(bool Debug, bool Launch, bool CreateShortcut)
{
    public bool RequiresInteractiveWindow => !Launch && !CreateShortcut;

    public static ApplicationOptions Parse(IEnumerable<string> arguments) =>
        Parse(arguments, Path.GetFileName(Environment.ProcessPath));

    public static ApplicationOptions Parse(IEnumerable<string> arguments, string? executableFileName)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        var values = arguments.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return new ApplicationOptions(
            values.Contains("--debug"),
            values.Contains("--launch") || string.Equals(
                executableFileName,
                "gproxyt-startup.exe",
                StringComparison.OrdinalIgnoreCase),
            values.Contains("--create-shortcut"));
    }
}
