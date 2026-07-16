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
            .Single(element => (string?)element.Attribute("Text") == "单独使用代理打开你的 ChatGPT。");

        Assert.Equal("{DynamicResource TextFillColorSecondaryBrush}", (string?)hint.Attribute("Foreground"));
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
        Assert.Equal("{DynamicResource TextFillColorPrimaryBrush}", (string?)headerBrandLogo.Attribute("Fill"));
        Assert.Equal("Assets/gproxyt.png", (string?)logoMask.Attribute("ImageSource"));
        Assert.Equal("Gproxyt", (string?)headerBrandTitle.Attribute("Text"));
        Assert.Equal("Gproxyt", (string?)contentTitle.Attribute("Text"));
    }

    private static XDocument LoadMarkup() => XDocument.Load(Path.Combine(AppContext.BaseDirectory, "MainWindow.xaml"));

    private static XElement FindLaunchButton(XDocument document) => document
        .Descendants(Presentation + "Button")
        .Single(element => (string?)element.Attribute(Xaml + "Name") == "LaunchButton");
}
