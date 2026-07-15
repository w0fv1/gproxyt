using Gproxyt.Core;

namespace Gproxyt.Core.Tests;

public sealed class ChatGptInstallationTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), $"gproxyt-{Guid.NewGuid():N}");

    [Fact]
    public void FromRegistration_uses_manifest_entry_executable()
    {
        var executable = Path.Combine(root, "app", "ChatGPT.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(executable)!);
        File.WriteAllBytes(executable, []);

        var registration = new PackageRegistration("OpenAI.Codex_1.0.0.0_x64__2p2nqsd0c76g0", root);
        var installation = ChatGptInstallation.FromRegistration(registration);

        Assert.Equal(registration.PackageFullName, installation.PackageFullName);
        Assert.Equal(ChatGptPackage.FamilyName, installation.PackageFamilyName);
        Assert.Equal("OpenAI.Codex_2p2nqsd0c76g0!App", installation.AppUserModelId);
        Assert.Equal(Path.GetFullPath(root), installation.InstallLocation);
        Assert.Equal(Path.GetFullPath(executable), installation.ExecutablePath);
    }

    [Fact]
    public void FromRegistration_rejects_missing_executable()
    {
        var registration = new PackageRegistration("OpenAI.Codex_1.0.0.0_x64__2p2nqsd0c76g0", root);

        Assert.Throws<FileNotFoundException>(() => ChatGptInstallation.FromRegistration(registration));
    }

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, true);
        }
    }
}
