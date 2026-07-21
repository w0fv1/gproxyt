using System.Xml.Linq;

namespace Gproxyt.Windows.Tests;

public sealed class MainWindowMarkupTests
{
    private static readonly XNamespace Presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
    private static readonly XNamespace Xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

    [Fact]
    public void Launch_button_uses_a_circular_keyboard_focus_visual()
    {
        var document = LoadMarkup();
        var launchButton = FindLaunchButton(document);
        var focusStyle = document
            .Descendants(Presentation + "Style")
            .Single(element => (string?)element.Attribute(Xaml + "Key") == "LaunchButtonFocusVisualStyle");
        var focusEllipses = focusStyle.Descendants(Presentation + "Ellipse").ToArray();

        Assert.Equal("{StaticResource LaunchButtonFocusVisualStyle}", (string?)launchButton.Attribute("FocusVisualStyle"));
        Assert.Equal(2, focusEllipses.Length);
        Assert.Contains(focusEllipses, element => (string?)element.Attribute("Stroke") == "{DynamicResource FocusStrokeColorOuterBrush}");
        Assert.Contains(focusEllipses, element => (string?)element.Attribute("Stroke") == "{DynamicResource FocusStrokeColorInnerBrush}");
    }

    [Fact]
    public void Launch_button_explains_its_proxy_only_behavior()
    {
        var document = LoadMarkup();
        var launchButton = FindLaunchButton(document);
        var hint = launchButton
            .ElementsAfterSelf(Presentation + "TextBlock")
            .Single(element => (string?)element.Attribute("Text") == "{Binding [LaunchHint], Source={x:Static local:AppLocalization.Current}}");

        Assert.Equal("{DynamicResource TextFillColorSecondaryBrush}", (string?)hint.Attribute("Foreground"));

        var proxyEndpoint = document
            .Descendants(Presentation + "TextBlock")
            .Single(element => (string?)element.Attribute(Xaml + "Name") == "ProxyEndpointText");
        Assert.Equal("{DynamicResource TextFillColorSecondaryBrush}", (string?)proxyEndpoint.Attribute("Foreground"));
    }

    [Fact]
    public void Brand_identity_is_split_between_the_header_lockup_and_content_title()
    {
        var document = LoadMarkup();
        var headerBrandLogo = document
            .Descendants(Presentation + "Rectangle")
            .Single(element => (string?)element.Attribute(Xaml + "Name") == "HeaderBrandLogo");
        var logoMask = headerBrandLogo
            .Descendants(Presentation + "ImageBrush")
            .Single();
        var headerBrandTitle = document
            .Descendants(Presentation + "TextBlock")
            .Single(element => (string?)element.Attribute(Xaml + "Name") == "HeaderBrandTitle");
        var contentTitle = document
            .Descendants(Presentation + "TextBlock")
            .Single(element => (string?)element.Attribute(Xaml + "Name") == "ContentTitle");

        Assert.Equal("18", (string?)headerBrandLogo.Attribute("Width"));
        Assert.Equal("18", (string?)headerBrandLogo.Attribute("Height"));
        Assert.Equal("#FF000000", (string?)headerBrandLogo.Attribute("Fill"));
        Assert.Equal("Assets/gproxyt.png", (string?)logoMask.Attribute("ImageSource"));
        Assert.Equal("GProxyT", (string?)headerBrandTitle.Attribute("Text"));
        Assert.Equal("GProxyT", (string?)contentTitle.Attribute("Text"));
    }

    [Fact]
    public void Settings_window_exposes_language_selection_inside_a_bordered_panel()
    {
        var document = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "SettingsWindow.xaml"));
        var languageSelector = document
            .Descendants(Presentation + "ComboBox")
            .Single(element => (string?)element.Attribute(Xaml + "Name") == "LanguageComboBox");
        var settingsPanel = document
            .Descendants(Presentation + "Border")
            .Single(element => (string?)element.Attribute(Xaml + "Name") == "SettingsPanel");

        Assert.Equal("{Binding [Language], Source={x:Static local:AppLocalization.Current}}", (string?)languageSelector.Attribute("AutomationProperties.Name"));
        Assert.Equal("1", (string?)settingsPanel.Attribute("BorderThickness"));
        Assert.Equal("20", (string?)settingsPanel.Attribute("Padding"));
    }

    private static XDocument LoadMarkup() => XDocument.Load(Path.Combine(AppContext.BaseDirectory, "MainWindow.xaml"));

    private static XElement FindLaunchButton(XDocument document) => document
        .Descendants(Presentation + "Button")
        .Single(element => (string?)element.Attribute(Xaml + "Name") == "LaunchButton");
}
