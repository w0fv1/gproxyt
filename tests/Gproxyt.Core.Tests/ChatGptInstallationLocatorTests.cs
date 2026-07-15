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
        var registration = new PackageRegistration("OpenAI.Codex_1.0.0.0_x64__2p2nqsd0c76g0", root);
        var locator = new ChatGptInstallationLocator(new StubPackageRegistrationSource(registration));

        var installation = locator.Locate();

        Assert.Equal(Path.GetFullPath(executable), installation.ExecutablePath);
    }

    [Fact]
    public void Locate_reports_missing_store_package()
    {
        var locator = new ChatGptInstallationLocator(new StubPackageRegistrationSource());

        Assert.Throws<InvalidOperationException>(() => locator.Locate());
    }

    [Fact]
    public void Locate_rejects_ambiguous_current_user_registrations()
    {
        var locator = new ChatGptInstallationLocator(new StubPackageRegistrationSource(
            new PackageRegistration("OpenAI.Codex_1.0.0.0_x64__2p2nqsd0c76g0", root),
            new PackageRegistration("OpenAI.Codex_2.0.0.0_x64__2p2nqsd0c76g0", root)));

        var exception = Assert.Throws<InvalidOperationException>(() => locator.Locate());

        Assert.Contains("更新", exception.Message);
    }

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, true);
        }
    }

    private sealed class StubPackageRegistrationSource(params PackageRegistration[] registrations) : IPackageRegistrationSource
    {
        public IReadOnlyList<PackageRegistration> FindCurrentUserRegistrations(string packageFamilyName)
        {
            Assert.Equal(ChatGptPackage.FamilyName, packageFamilyName);
            return registrations;
        }
    }
}
