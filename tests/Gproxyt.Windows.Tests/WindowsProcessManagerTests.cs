using Gproxyt.Core;

namespace Gproxyt.Windows.Tests;

public sealed class WindowsProcessManagerTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), $"gproxyt-activation-{Guid.NewGuid():N}");

    [Fact]
    public void Start_injects_environment_activates_aumid_verifies_identity_and_restores_package_state()
    {
        var events = new List<string>();
        var installation = CreateInstallation();
        var api = new RecordingPackageActivationApi(events, ChatGptPackage.FamilyName);
        var manager = new WindowsProcessManager(api);
        var plan = ProxyLaunchPlan.Create(ProxyEndpoint.Parse("127.0.0.1:7890"));

        var processId = manager.Start(installation, plan);

        Assert.Equal(42, processId);
        Assert.Equal(["enable", "activate", "verify", "disable"], events);
        Assert.Equal(installation.PackageFullName, api.EnabledPackageFullName);
        Assert.Equal(installation.AppUserModelId, api.ActivatedAppUserModelId);
        Assert.Equal(string.Join(' ', plan.Arguments), api.ActivationArguments);
        Assert.Equal(plan.Environment, api.Environment);
    }

    [Fact]
    public void Start_restores_package_state_when_activation_fails()
    {
        var events = new List<string>();
        var installation = CreateInstallation();
        var api = new RecordingPackageActivationApi(events, ChatGptPackage.FamilyName)
        {
            ActivationFailure = new InvalidOperationException("activation failed")
        };
        var manager = new WindowsProcessManager(api);

        Assert.Throws<PackageLaunchTargetUnavailableException>(() =>
            manager.Start(installation, ProxyLaunchPlan.Create(ProxyEndpoint.Parse("127.0.0.1:7890"))));

        Assert.Equal(["enable", "activate", "disable"], events);
    }

    [Fact]
    public void Start_does_not_disable_when_environment_injection_fails()
    {
        var events = new List<string>();
        var installation = CreateInstallation();
        var api = new RecordingPackageActivationApi(events, ChatGptPackage.FamilyName)
        {
            EnableFailure = new InvalidOperationException("enable failed")
        };
        var manager = new WindowsProcessManager(api);

        Assert.Throws<PackageLaunchTargetUnavailableException>(() =>
            manager.Start(installation, ProxyLaunchPlan.Create(ProxyEndpoint.Parse("127.0.0.1:7890"))));

        Assert.Equal(["enable"], events);
    }

    [Fact]
    public void Start_rejects_a_process_from_another_package_family_and_restores_package_state()
    {
        var events = new List<string>();
        var installation = CreateInstallation();
        var api = new RecordingPackageActivationApi(events, "Other.Package_family");
        var manager = new WindowsProcessManager(api);

        var exception = Assert.Throws<PackageLaunchTargetUnavailableException>(() =>
            manager.Start(installation, ProxyLaunchPlan.Create(ProxyEndpoint.Parse("127.0.0.1:7890"))));

        Assert.Contains("程序包身份", exception.Message);
        Assert.Equal(["enable", "activate", "verify", "disable"], events);
    }

    [Fact]
    public void Start_reports_package_state_restoration_failure()
    {
        var events = new List<string>();
        var installation = CreateInstallation();
        var api = new RecordingPackageActivationApi(events, ChatGptPackage.FamilyName)
        {
            DisableFailure = new InvalidOperationException("disable failed")
        };
        var manager = new WindowsProcessManager(api);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            manager.Start(installation, ProxyLaunchPlan.Create(ProxyEndpoint.Parse("127.0.0.1:7890"))));

        Assert.Contains("程序包状态", exception.Message);
        Assert.Equal(["enable", "activate", "verify", "disable"], events);
    }

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, true);
        }
    }

    private ChatGptInstallation CreateInstallation()
    {
        var executable = Path.Combine(root, "app", "ChatGPT.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(executable)!);
        File.WriteAllBytes(executable, []);
        return ChatGptInstallation.FromRegistration(
            new PackageRegistration("OpenAI.Codex_1.0.0.0_x64__2p2nqsd0c76g0", root));
    }

    private sealed class RecordingPackageActivationApi(ICollection<string> events, string? processPackageFamilyName)
        : IWindowsPackageActivationApi
    {
        public Exception? EnableFailure { get; init; }
        public Exception? ActivationFailure { get; init; }
        public Exception? DisableFailure { get; init; }
        public string? EnabledPackageFullName { get; private set; }
        public string? ActivatedAppUserModelId { get; private set; }
        public string? ActivationArguments { get; private set; }
        public IReadOnlyDictionary<string, string>? Environment { get; private set; }

        public void EnableDebugging(string packageFullName, IReadOnlyDictionary<string, string> environment)
        {
            events.Add("enable");
            if (EnableFailure is not null)
            {
                throw EnableFailure;
            }
            EnabledPackageFullName = packageFullName;
            Environment = environment;
        }

        public int ActivateApplication(string appUserModelId, string arguments)
        {
            events.Add("activate");
            if (ActivationFailure is not null)
            {
                throw ActivationFailure;
            }
            ActivatedAppUserModelId = appUserModelId;
            ActivationArguments = arguments;
            return 42;
        }

        public string? GetProcessPackageFamilyName(int processId)
        {
            Assert.Equal(42, processId);
            events.Add("verify");
            return processPackageFamilyName;
        }

        public void DisableDebugging(string packageFullName)
        {
            Assert.Equal(EnabledPackageFullName, packageFullName);
            events.Add("disable");
            if (DisableFailure is not null)
            {
                throw DisableFailure;
            }
        }
    }
}
