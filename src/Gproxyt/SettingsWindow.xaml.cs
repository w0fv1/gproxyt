using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;
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
        FlowDirection = AppLocalization.Current.FlowDirection;
        Language = XmlLanguage.GetLanguage(AppLocalization.Current.Culture.IetfLanguageTag);
        ProxyUrlTextBox.Text = settings.ProxyUrl;
        StartWithWindowsToggle.IsChecked = settings.StartWithWindows;
        LanguageComboBox.ItemsSource = AppLocalization.SupportedCultures;
        LanguageComboBox.SelectedValue = AppLocalization.ResolveConfiguredCulture(
            settings.CultureName,
            AppLocalization.Current.Culture).Name;
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
                StartWithWindowsToggle.IsChecked == true,
                ((System.Globalization.CultureInfo)LanguageComboBox.SelectedItem).Name);
            DialogResult = true;
        }
        catch (Exception)
        {
            MessageBox.Show(this, AppLocalization.Current["InvalidProxySettings"], "GProxyT", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Cancel(object sender, RoutedEventArgs eventArgs) => DialogResult = false;
}
