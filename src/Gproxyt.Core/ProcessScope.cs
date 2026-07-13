namespace Gproxyt.Core;

public sealed class ProcessScope
{
    private readonly string root;

    public ProcessScope(string installLocation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(installLocation);
        root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(installLocation));
    }

    public bool Contains(string? executablePath)
    {
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            return false;
        }

        var fullPath = Path.GetFullPath(executablePath);
        return fullPath.StartsWith($"{root}{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase);
    }
}
