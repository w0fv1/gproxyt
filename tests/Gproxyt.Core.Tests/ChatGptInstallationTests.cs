using Gproxyt.Core;

namespace Gproxyt.Core.Tests;

public sealed class ChatGptInstallationTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), $"gproxyt-{Guid.NewGuid():N}");

    [Fact]
    public void FromInstallLocation_uses_manifest_entry_executable()
    {
        var executable = Path.Combine(root, "app", "ChatGPT.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(executable)!);
        File.WriteAllBytes(executable, []);

        var installation = ChatGptInstallation.FromInstallLocation(root);

        Assert.Equal(Path.GetFullPath(root), installation.InstallLocation);
        Assert.Equal(Path.GetFullPath(executable), installation.ExecutablePath);
    }

    [Fact]
    public void FromInstallLocation_rejects_missing_executable()
    {
        Assert.Throws<FileNotFoundException>(() => ChatGptInstallation.FromInstallLocation(root));
    }

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, true);
        }
    }
}
