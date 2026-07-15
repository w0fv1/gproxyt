using Gproxyt.Core;

namespace Gproxyt.Windows.Tests;

public sealed class WindowsProcessManagerTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), $"gproxyt-activation-{Guid.NewGuid():N}");

    [Fact]
    public void Start_activates_aumid_verifies_identity_and_waits_for_stability()
    {
        var events = new List<string>();
        var installation = CreateInstallation();
        var api = new RecordingPackageActivationApi(events, ChatGptPackage.FamilyName);
        var manager = new WindowsProcessManager(api);
        var plan = ProxyLaunchPlan.Create(ProxyEndpoint.Parse("127.0.0.1:7890"));

        var processId = manager.Start(installation, plan);

        Assert.Equal(42, processId);
        Assert.Equal(["activate", "verify", "stabilize", "show-window"], events);
        Assert.Equal(installation.AppUserModelId, api.ActivatedAppUserModelId);
        Assert.Equal(string.Join(' ', plan.Arguments), api.ActivationArguments);
    }

    [Fact]
    public void Start_reports_activation_failure()
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

        Assert.Equal(["activate"], events);
    }

    [Fact]
    public void Start_rejects_a_process_that_exits_during_stabilization()
    {
        var events = new List<string>();
        var installation = CreateInstallation();
        var api = new RecordingPackageActivationApi(events, ChatGptPackage.FamilyName)
        {
            ExitsDuringStabilization = true
        };
        var manager = new WindowsProcessManager(api);

        var exception = Assert.Throws<PackageLaunchTargetUnavailableException>(() =>
            manager.Start(installation, ProxyLaunchPlan.Create(ProxyEndpoint.Parse("127.0.0.1:7890"))));

        Assert.Contains("启动后立即退出", exception.Message);
        Assert.Equal(["activate", "verify", "stabilize"], events);
    }

    [Fact]
    public void Start_rejects_a_process_from_another_package_family()
    {
        var events = new List<string>();
        var installation = CreateInstallation();
        var api = new RecordingPackageActivationApi(events, "Other.Package_family");
        var manager = new WindowsProcessManager(api);

        var exception = Assert.Throws<PackageLaunchTargetUnavailableException>(() =>
            manager.Start(installation, ProxyLaunchPlan.Create(ProxyEndpoint.Parse("127.0.0.1:7890"))));

        Assert.Contains("程序包身份", exception.Message);
        Assert.Equal(["activate", "verify"], events);
    }

    [Fact]
    public void Start_rejects_an_application_without_a_visible_window()
    {
        var events = new List<string>();
        var installation = CreateInstallation();
        var api = new RecordingPackageActivationApi(events, ChatGptPackage.FamilyName)
        {
            WindowVisible = false
        };
        var manager = new WindowsProcessManager(api);

        var exception = Assert.Throws<PackageLaunchTargetUnavailableException>(() =>
            manager.Start(installation, ProxyLaunchPlan.Create(ProxyEndpoint.Parse("127.0.0.1:7890"))));

        Assert.Contains("窗口", exception.Message);
        Assert.Equal(["activate", "verify", "stabilize", "show-window"], events);
    }

    [Fact]
    public void Stop_terminates_the_registered_package()
    {
        var events = new List<string>();
        var installation = CreateInstallation();
        var api = new RecordingPackageActivationApi(events, ChatGptPackage.FamilyName);
        var manager = new WindowsProcessManager(api);

        manager.Stop(installation);

        Assert.Equal(["terminate", "wait-terminated"], events);
        Assert.Equal(installation.PackageFullName, api.TerminatedPackageFullName);
    }

    [Fact]
    public void Stop_rejects_a_package_that_does_not_finish_terminating()
    {
        var events = new List<string>();
        var installation = CreateInstallation();
        var api = new RecordingPackageActivationApi(events, ChatGptPackage.FamilyName)
        {
            PackageExited = false
        };
        var manager = new WindowsProcessManager(api);

        var exception = Assert.Throws<InvalidOperationException>(() => manager.Stop(installation));

        Assert.Contains("退出", exception.Message);
        Assert.Equal(["terminate", "wait-terminated"], events);
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
        public Exception? ActivationFailure { get; init; }
        public bool ExitsDuringStabilization { get; init; }
        public bool PackageExited { get; init; } = true;
        public bool WindowVisible { get; init; } = true;
        public string? ActivatedAppUserModelId { get; private set; }
        public string? ActivationArguments { get; private set; }
        public string? TerminatedPackageFullName { get; private set; }

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

        public bool WaitForProcessExit(int processId, int milliseconds)
        {
            Assert.Equal(42, processId);
            Assert.True(milliseconds > 0);
            events.Add("stabilize");
            return ExitsDuringStabilization;
        }

        public void TerminateAllProcesses(string packageFullName)
        {
            events.Add("terminate");
            TerminatedPackageFullName = packageFullName;
        }

        public bool WaitForPackageExit(string packageFamilyName, int milliseconds)
        {
            Assert.Equal(ChatGptPackage.FamilyName, packageFamilyName);
            Assert.True(milliseconds > 0);
            events.Add("wait-terminated");
            return PackageExited;
        }

        public bool EnsureProcessWindowVisible(int processId, int milliseconds)
        {
            Assert.Equal(42, processId);
            Assert.True(milliseconds > 0);
            events.Add("show-window");
            return WindowVisible;
        }
    }
}
