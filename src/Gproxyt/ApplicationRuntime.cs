using Gproxyt.Core;

namespace Gproxyt;

internal sealed class ApplicationRuntime
{
    private readonly LauncherSettingsStore settingsStore = AppPaths.CreateSettingsStore();
    private readonly IChatGptInstallationLocator installationLocator;
    private readonly GproxytLauncher launcher;
    private readonly WindowsStartupRegistration startupRegistration = new();

    public ApplicationRuntime()
    {
        installationLocator = new ChatGptInstallationLocator(new WindowsPackageRegistrationSource());
        launcher = new GproxytLauncher(installationLocator, new WindowsProcessManager());
        Diagnostics = new EnvironmentDiagnostics(installationLocator);
    }

    public EnvironmentDiagnostics Diagnostics { get; }

    public LauncherSettings LoadSettings() => settingsStore.Load();

    public void SynchronizeStartup() => startupRegistration.Apply(LoadSettings().StartWithWindows);

    public LauncherSettings SaveSettings(LauncherSettings settings)
    {
        var normalized = settings.Normalize();
        settingsStore.Save(normalized);
        startupRegistration.Apply(normalized.StartWithWindows);
        return normalized;
    }

    public async Task<LaunchResult> LaunchAsync(LauncherSettings settings)
    {
        var normalized = SaveSettings(settings);
        var proxy = ProxyEndpoint.Parse(normalized.ProxyUrl);
        await Diagnostics.EnsureProxyReachableAsync(proxy);
        return await launcher.LaunchAsync(normalized);
    }

    public string CreateShortcut()
    {
        var installation = installationLocator.Locate();
        return ShortcutService.Create(installation.ExecutablePath);
    }
}
