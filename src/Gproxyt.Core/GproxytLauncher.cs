namespace Gproxyt.Core;

public sealed class GproxytLauncher(IChatGptInstallationLocator installationLocator, IProcessManager processManager)
{
    public Task<LaunchResult> LaunchAsync(LauncherSettings settings, CancellationToken cancellationToken = default) =>
        Task.Run(() => Launch(settings), cancellationToken);

    public LaunchResult Launch(LauncherSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var normalized = settings.Normalize();
        var proxy = ProxyEndpoint.Parse(normalized.ProxyUrl);
        var installation = installationLocator.Locate();
        var stoppedProcessCount = normalized.RestartExisting
            ? processManager.Stop(new ProcessScope(installation.InstallLocation))
            : 0;
        var plan = ProxyLaunchPlan.Create(installation.ExecutablePath, proxy);
        var processId = processManager.Start(plan);
        return new LaunchResult(installation, proxy, stoppedProcessCount, processId);
    }
}
