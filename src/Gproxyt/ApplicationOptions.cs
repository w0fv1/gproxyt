namespace Gproxyt;

internal sealed record ApplicationOptions(bool Debug, bool Launch, bool CreateShortcut)
{
    public bool RequiresInteractiveWindow => !Launch && !CreateShortcut;

    public static ApplicationOptions Parse(IEnumerable<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        var values = arguments.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return new ApplicationOptions(
            values.Contains("--debug"),
            values.Contains("--launch"),
            values.Contains("--create-shortcut"));
    }
}
