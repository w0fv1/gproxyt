using Gproxyt.Core;

namespace Gproxyt.Core.Tests;

public sealed class ChatGptInstallationLocatorTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), $"gproxyt-package-{Guid.NewGuid():N}");

    [Fact]
    public void Locate_builds_installation_from_package_source()
    {
        var executable = Path.Combine(root, "app", "ChatGPT.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(executable)!);
        File.WriteAllBytes(executable, []);
        var locator = new ChatGptInstallationLocator(new StubPackageLocationSource(root));

        var installation = locator.Locate();

        Assert.Equal(Path.GetFullPath(executable), installation.ExecutablePath);
    }

    [Fact]
    public void Locate_reports_missing_store_package()
    {
        var locator = new ChatGptInstallationLocator(new StubPackageLocationSource(null));

        Assert.Throws<InvalidOperationException>(() => locator.Locate());
    }

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, true);
        }
    }

    private sealed class StubPackageLocationSource(string? value) : IPackageLocationSource
    {
        public string? GetLatestInstallLocation() => value;
    }
}
