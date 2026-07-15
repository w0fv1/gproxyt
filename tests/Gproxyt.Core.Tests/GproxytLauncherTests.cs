using Gproxyt.Core;

namespace Gproxyt.Core.Tests;

public sealed class GproxytLauncherTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), $"gproxyt-launch-{Guid.NewGuid():N}");

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    public void Launch_applies_restart_policy_and_starts_proxy_plan(bool restartExisting, int expectedStopCalls)
    {
        var events = new List<string>();
        var installation = CreateInstallation();
        var processes = new RecordingProcessManager(events);
        var launcher = new GproxytLauncher(new StubInstallationLocator(events, installation), processes);

        var result = launcher.Launch(new LauncherSettings("127.0.0.1:7890", restartExisting, false));

        Assert.Equal(restartExisting ? ["locate", "stop", "start"] : ["locate", "start"], events);
        Assert.Equal(expectedStopCalls, processes.StopCalls);
        var started = Assert.Single(processes.Starts);
        Assert.Equal(installation, started.Installation);
        Assert.Contains("--proxy-server=http://127.0.0.1:7890", started.Plan.Arguments);
        Assert.Equal(42, result.ProcessId);
        Assert.Equal(installation, result.Installation);
    }

    [Fact]
    public void Launch_re_resolves_registration_once_when_the_package_switches_during_start()
    {
        var events = new List<string>();
        var oldInstallation = CreateInstallation("1.0.0.0", Path.Combine(root, "old"));
        var currentInstallation = CreateInstallation("2.0.0.0", Path.Combine(root, "current"));
        var processes = new RecordingProcessManager(
            events,
            new PackageLaunchTargetUnavailableException("启动目标不可用。", new IOException()));
        var launcher = new GproxytLauncher(
            new StubInstallationLocator(events, oldInstallation, currentInstallation),
            processes);

        var result = launcher.Launch(new LauncherSettings("127.0.0.1:7890", true, false));

        Assert.Equal(["locate", "stop", "start", "locate", "stop", "start"], events);
        Assert.Equal([oldInstallation, currentInstallation], processes.Starts.Select(start => start.Installation));
        Assert.Equal(currentInstallation, result.Installation);
        Assert.Equal(42, result.ProcessId);
    }

    [Fact]
    public void Launch_does_not_retry_a_second_package_switch_failure()
    {
        var events = new List<string>();
        var oldInstallation = CreateInstallation("1.0.0.0", Path.Combine(root, "old"));
        var currentInstallation = CreateInstallation("2.0.0.0", Path.Combine(root, "current"));
        var processes = new RecordingProcessManager(
            events,
            new PackageLaunchTargetUnavailableException("启动目标不可用。", new IOException()),
            new PackageLaunchTargetUnavailableException("启动目标仍不可用。", new IOException()));
        var launcher = new GproxytLauncher(
            new StubInstallationLocator(events, oldInstallation, currentInstallation),
            processes);

        Assert.Throws<PackageLaunchTargetUnavailableException>(() =>
            launcher.Launch(new LauncherSettings("127.0.0.1:7890", false, false)));

        Assert.Equal(["locate", "start", "locate", "start"], events);
        Assert.Equal(2, processes.Starts.Count);
    }

    [Fact]
    public void Launch_does_not_retry_when_the_registered_package_is_unchanged()
    {
        var events = new List<string>();
        var installation = CreateInstallation();
        var failure = new PackageLaunchTargetUnavailableException("启动失败。", new IOException());
        var processes = new RecordingProcessManager(events, failure);
        var launcher = new GproxytLauncher(
            new StubInstallationLocator(events, installation, installation),
            processes);

        var thrown = Assert.Throws<PackageLaunchTargetUnavailableException>(() =>
            launcher.Launch(new LauncherSettings("127.0.0.1:7890", false, false)));

        Assert.Same(failure, thrown);
        Assert.Equal(["locate", "start", "locate"], events);
        Assert.Single(processes.Starts);
    }

    [Fact]
    public async Task LaunchAsync_keeps_blocking_process_work_off_the_calling_thread()
    {
        var installation = CreateInstallation();
        using var processes = new BlockingProcessManager();
        var launcher = new GproxytLauncher(new StubInstallationLocator([], installation), processes);

        var launchTask = launcher.LaunchAsync(
            new LauncherSettings("127.0.0.1:7890", false, false),
            TestContext.Current.CancellationToken);

        Assert.True(processes.StartEntered.Wait(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken));
        Assert.False(launchTask.IsCompleted);
        processes.Release.Set();
        var result = await launchTask;
        Assert.Equal(42, result.ProcessId);
    }

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, true);
        }
    }

    private ChatGptInstallation CreateInstallation(string version = "1.0.0.0", string? installLocation = null)
    {
        installLocation ??= root;
        var executable = Path.Combine(installLocation, "app", "ChatGPT.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(executable)!);
        File.WriteAllBytes(executable, []);
        return ChatGptInstallation.FromRegistration(
            new PackageRegistration($"OpenAI.Codex_{version}_x64__2p2nqsd0c76g0", installLocation));
    }

    private sealed class StubInstallationLocator : IChatGptInstallationLocator
    {
        private readonly ICollection<string> events;
        private readonly Queue<ChatGptInstallation> installations;

        public StubInstallationLocator(ICollection<string> events, params ChatGptInstallation[] installations)
        {
            this.events = events;
            this.installations = new Queue<ChatGptInstallation>(installations);
        }

        public ChatGptInstallation Locate()
        {
            events.Add("locate");
            return installations.Dequeue();
        }
    }

    private sealed class RecordingProcessManager : IProcessManager
    {
        private readonly ICollection<string> events;
        private readonly Queue<Exception> startFailures;

        public RecordingProcessManager(ICollection<string> events, params Exception[] startFailures)
        {
            this.events = events;
            this.startFailures = new Queue<Exception>(startFailures);
        }

        public int StopCalls { get; private set; }
        public List<(ChatGptInstallation Installation, ProxyLaunchPlan Plan)> Starts { get; } = [];

        public void Stop(ChatGptInstallation installation)
        {
            Assert.Equal(ChatGptPackage.FamilyName, installation.PackageFamilyName);
            events.Add("stop");
            StopCalls++;
        }

        public int Start(ChatGptInstallation installation, ProxyLaunchPlan plan)
        {
            events.Add("start");
            Starts.Add((installation, plan));
            if (startFailures.Count > 0)
            {
                throw startFailures.Dequeue();
            }
            return 42;
        }
    }

    private sealed class BlockingProcessManager : IProcessManager, IDisposable
    {
        public ManualResetEventSlim StartEntered { get; } = new(false);
        public ManualResetEventSlim Release { get; } = new(false);

        public void Stop(ChatGptInstallation installation)
        {
        }

        public int Start(ChatGptInstallation installation, ProxyLaunchPlan plan)
        {
            StartEntered.Set();
            Release.Wait();
            return 42;
        }

        public void Dispose()
        {
            StartEntered.Dispose();
            Release.Dispose();
        }
    }
}
