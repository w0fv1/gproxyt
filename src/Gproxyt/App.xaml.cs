using System.Windows;
using System.Globalization;
using Wpf.Ui.Appearance;

namespace Gproxyt;

public partial class App : Application
{
    private const string InteractiveInstanceId = "gproxyt";
    private IApplicationLog log = ApplicationLog.None;
    private SingleInstanceCoordinator? singleInstance;

    protected override async void OnStartup(StartupEventArgs eventArgs)
    {
        base.OnStartup(eventArgs);
        AppLocalization.Initialize(this, CultureInfo.CurrentUICulture);
        var options = ApplicationOptions.Parse(eventArgs.Args);
        try
        {
            log = ApplicationLog.Create(options.Debug, Environment.CurrentDirectory);
            log.Information(
                "application_started",
                ("Version", typeof(App).Assembly.GetName().Version?.ToString()),
                ("CurrentDirectory", Environment.CurrentDirectory),
                ("Debug", options.Debug),
                ("Launch", options.Launch),
                ("CreateShortcut", options.CreateShortcut),
                ("LogFile", log.FilePath));
            if (options.RequiresInteractiveWindow)
            {
                singleInstance = new SingleInstanceCoordinator(InteractiveInstanceId);
                if (!singleInstance.IsPrimary)
                {
                    var foregroundPermissionGranted =
                        WindowsForegroundActivation.GrantExistingInstanceForegroundPermission();
                    singleInstance.NotifyPrimary();
                    log.Information(
                        "existing_instance_activation_requested",
                        ("ForegroundPermissionGranted", foregroundPermissionGranted));
                    Shutdown();
                    return;
                }
                log.Information("primary_instance_acquired");
            }
            ApplicationThemeManager.ApplySystemTheme();
            var runtime = new ApplicationRuntime(log);
            await runtime.SynchronizeStartupAsync();
            if (options.Launch)
            {
                await runtime.LaunchAsync(runtime.LoadSettings());
                Shutdown();
                return;
            }

            if (options.CreateShortcut)
            {
                runtime.CreateShortcut();
                Shutdown();
                return;
            }

            var mainWindow = new MainWindow(runtime);
            MainWindow = mainWindow;
            mainWindow.Show();
            singleInstance!.StartListening(() =>
            {
                if (Dispatcher.HasShutdownStarted)
                {
                    return;
                }
                Dispatcher.BeginInvoke(() =>
                {
                    try
                    {
                        var activated = mainWindow.RestoreAndActivate();
                        log.Information("existing_instance_activated", ("ForegroundActivated", activated));
                    }
                    catch (Exception exception)
                    {
                        log.Error(exception, "existing_instance_activation_failed");
                    }
                });
            });
        }
        catch (Exception exception)
        {
            log.Error(exception, "application_failed");
            MessageBox.Show(AppLocalization.Current["StartupFailed"], "GProxyT", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs eventArgs)
    {
        singleInstance?.Dispose();
        log.Information("application_exited", ("ExitCode", eventArgs.ApplicationExitCode));
        log.Dispose();
        base.OnExit(eventArgs);
    }
}
