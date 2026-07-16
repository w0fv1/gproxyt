using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;
using Gproxyt.Core;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;

namespace Gproxyt;

public partial class MainWindow : FluentWindow
{
    private readonly ApplicationRuntime runtime;
    private LauncherSettings settings;
    private SettingsWindow? settingsWindow;

    internal MainWindow(ApplicationRuntime runtime)
    {
        this.runtime = runtime;
        settings = runtime.LoadSettings();
        InitializeComponent();
        FlowDirection = AppLocalization.Current.FlowDirection;
        Language = XmlLanguage.GetLanguage(AppLocalization.Current.Culture.IetfLanguageTag);
        SystemThemeWatcher.Watch(this);
    }

    private void DragWindow(object sender, MouseButtonEventArgs eventArgs)
    {
        if (eventArgs.ChangedButton == MouseButton.Left)
        {
            DragMove();
        }
    }

    private async void OpenSettings(object sender, RoutedEventArgs eventArgs)
    {
        if (settingsWindow is not null)
        {
            settingsWindow.Activate();
            return;
        }

        settingsWindow = new SettingsWindow(settings) { Owner = this };
        try
        {
            if (settingsWindow.ShowDialog() == true)
            {
                try
                {
                    settings = await runtime.SaveSettingsAsync(settingsWindow.Settings);
                }
                catch (Exception)
                {
                    MessageBox.Show(this, AppLocalization.Current["SettingsSaveFailed"], "GProxyT", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        finally
        {
            settingsWindow = null;
        }
    }

    private void MinimizeWindow(object sender, RoutedEventArgs eventArgs) => WindowState = WindowState.Minimized;

    private void CloseWindow(object sender, RoutedEventArgs eventArgs) => Close();

    internal bool RestoreAndActivate() =>
        WindowsForegroundActivation.RestoreAndActivate(settingsWindow is not null ? settingsWindow : this);

    private async void LaunchChatGpt(object sender, RoutedEventArgs eventArgs)
    {
        try
        {
            LaunchButton.IsEnabled = false;
            LaunchIcon.Visibility = Visibility.Collapsed;
            LaunchProgressRing.Visibility = Visibility.Visible;
            await runtime.LaunchAsync(settings);
        }
        catch (Exception)
        {
            MessageBox.Show(this, AppLocalization.Current["LaunchFailed"], "GProxyT", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            LaunchProgressRing.Visibility = Visibility.Collapsed;
            LaunchIcon.Visibility = Visibility.Visible;
            LaunchButton.IsEnabled = true;
        }
    }
}
