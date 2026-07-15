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
        if (normalized.RestartExisting)
        {
            processManager.Stop(installation);
        }
        var plan = ProxyLaunchPlan.Create(proxy);
        int processId;
        try
        {
            processId = processManager.Start(installation, plan);
        }
        catch (PackageLaunchTargetUnavailableException)
        {
            var currentInstallation = installationLocator.Locate();
            if (string.Equals(
                currentInstallation.PackageFullName,
                installation.PackageFullName,
                StringComparison.OrdinalIgnoreCase))
            {
                throw;
            }

            installation = currentInstallation;
            if (normalized.RestartExisting)
            {
                processManager.Stop(installation);
            }
            processId = processManager.Start(installation, plan);
        }
        return new LaunchResult(installation, proxy, processId);
    }
}
