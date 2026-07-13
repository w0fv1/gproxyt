using System.Windows;
using Wpf.Ui.Appearance;

namespace Gproxyt;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs eventArgs)
    {
        base.OnStartup(eventArgs);
        ApplicationThemeManager.ApplySystemTheme();
        var runtime = new ApplicationRuntime();
        try
        {
            runtime.SynchronizeStartup();
            if (eventArgs.Args.Contains("--launch", StringComparer.OrdinalIgnoreCase))
            {
                await runtime.LaunchAsync(runtime.LoadSettings());
                Shutdown();
                return;
            }

            if (eventArgs.Args.Contains("--create-shortcut", StringComparer.OrdinalIgnoreCase))
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
            MessageBox.Show(exception.Message, "gproxyt", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }
}
