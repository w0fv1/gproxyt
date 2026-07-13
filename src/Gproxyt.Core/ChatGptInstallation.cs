namespace Gproxyt.Core;

public sealed record ChatGptInstallation(string InstallLocation, string ExecutablePath)
{
    public const string PackageName = "OpenAI.Codex";
    public const string RelativeExecutablePath = "app\\ChatGPT.exe";

    public static ChatGptInstallation FromInstallLocation(string installLocation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(installLocation);

        var fullLocation = Path.GetFullPath(installLocation.Trim());
        var executablePath = Path.GetFullPath(Path.Combine(fullLocation, RelativeExecutablePath));
        if (!File.Exists(executablePath))
        {
            throw new FileNotFoundException("没有在 Microsoft Store 包中找到 ChatGPT.exe。", executablePath);
        }

        return new ChatGptInstallation(fullLocation, executablePath);
    }
}
