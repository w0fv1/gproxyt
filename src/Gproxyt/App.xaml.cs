using System.Windows;
using Wpf.Ui.Appearance;

namespace Gproxyt;

public partial class App : Application
{
    private IApplicationLog log = ApplicationLog.None;

    protected override async void OnStartup(StartupEventArgs eventArgs)
    {
        base.OnStartup(eventArgs);
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
            ApplicationThemeManager.ApplySystemTheme();
            var runtime = new ApplicationRuntime(log);
            runtime.SynchronizeStartup();
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

            MainWindow = new MainWindow(runtime);
            MainWindow.Show();
        }
        catch (Exception exception)
        {
            log.Error(exception, "application_failed");
            MessageBox.Show(exception.Message, "gproxyt", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs eventArgs)
    {
        log.Information("application_exited", ("ExitCode", eventArgs.ApplicationExitCode));
        log.Dispose();
        base.OnExit(eventArgs);
    }
}
