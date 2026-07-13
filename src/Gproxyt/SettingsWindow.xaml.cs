using System.Windows;
using System.Windows.Input;
using Gproxyt.Core;
using Wpf.Ui.Controls;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;

namespace Gproxyt;

public partial class SettingsWindow : FluentWindow
{
    private readonly LauncherSettings currentSettings;

    internal SettingsWindow(LauncherSettings settings)
    {
        currentSettings = settings;
        Settings = settings;
        InitializeComponent();
        ProxyUrlTextBox.Text = settings.ProxyUrl;
        StartWithWindowsToggle.IsChecked = settings.StartWithWindows;
    }

    internal LauncherSettings Settings { get; private set; }

    private void DragWindow(object sender, MouseButtonEventArgs eventArgs)
    {
        if (eventArgs.ChangedButton == MouseButton.Left)
        {
            DragMove();
        }
    }

    private void Save(object sender, RoutedEventArgs eventArgs)
    {
        try
        {
            Settings = new LauncherSettings(
                ProxyEndpoint.Parse(ProxyUrlTextBox.Text).Value,
                currentSettings.RestartExisting,
                StartWithWindowsToggle.IsChecked == true);
            DialogResult = true;
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "gproxyt", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Cancel(object sender, RoutedEventArgs eventArgs) => DialogResult = false;
}
