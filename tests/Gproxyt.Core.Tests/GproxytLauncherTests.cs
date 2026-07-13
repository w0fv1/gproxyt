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
        var executable = Path.Combine(root, "app", "ChatGPT.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(executable)!);
        File.WriteAllBytes(executable, []);
        var installation = ChatGptInstallation.FromInstallLocation(root);
        var processes = new RecordingProcessManager();
        var launcher = new GproxytLauncher(new StubInstallationLocator(installation), processes);

        var result = launcher.Launch(new LauncherSettings("127.0.0.1:7890", restartExisting, false));

        Assert.Equal(expectedStopCalls, processes.StopCalls);
        Assert.NotNull(processes.StartedPlan);
        Assert.Equal("http://127.0.0.1:7890", processes.StartedPlan.Environment["HTTPS_PROXY"]);
        Assert.Equal(42, result.ProcessId);
        Assert.Equal(installation, result.Installation);
    }

    [Fact]
    public async Task LaunchAsync_keeps_blocking_process_work_off_the_calling_thread()
    {
        var executable = Path.Combine(root, "app", "ChatGPT.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(executable)!);
        File.WriteAllBytes(executable, []);
        var installation = ChatGptInstallation.FromInstallLocation(root);
        using var processes = new BlockingProcessManager();
        var launcher = new GproxytLauncher(new StubInstallationLocator(installation), processes);

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

    private sealed class StubInstallationLocator(ChatGptInstallation installation) : IChatGptInstallationLocator
    {
        public ChatGptInstallation Locate() => installation;
    }

    private sealed class RecordingProcessManager : IProcessManager
    {
        public int StopCalls { get; private set; }
        public ProxyLaunchPlan? StartedPlan { get; private set; }

        public int Stop(ProcessScope scope)
        {
            StopCalls++;
            return 3;
        }

        public int Start(ProxyLaunchPlan plan)
        {
            StartedPlan = plan;
            return 42;
        }
    }

    private sealed class BlockingProcessManager : IProcessManager, IDisposable
    {
        public ManualResetEventSlim StartEntered { get; } = new(false);
        public ManualResetEventSlim Release { get; } = new(false);

        public int Stop(ProcessScope scope) => 0;

        public int Start(ProxyLaunchPlan plan)
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
