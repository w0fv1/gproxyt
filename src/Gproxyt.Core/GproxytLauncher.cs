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
        var stoppedProcessCount = normalized.RestartExisting
            ? processManager.Stop(new PackageProcessScope(ChatGptPackage.FamilyName))
            : 0;
        var installation = installationLocator.Locate();
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
            processId = processManager.Start(installation, plan);
        }
        return new LaunchResult(installation, proxy, stoppedProcessCount, processId);
    }
}
