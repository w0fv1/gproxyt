using System.Text.Json;
using System.Text.RegularExpressions;

namespace Gproxyt.Windows.Tests;

public sealed partial class LocalizationResourcesTests
{
    private static readonly string[] Cultures =
    [
        "ar-SA", "de-DE", "en-US", "es-ES", "fr-FR", "hi-IN", "id-ID", "it-IT", "ja-JP", "ko-KR",
        "nl-NL", "pl-PL", "pt-BR", "ru-RU", "th-TH", "tr-TR", "uk-UA", "vi-VN", "zh-CN", "zh-TW"
    ];

    [Fact]
    public void Every_supported_culture_has_the_same_complete_resource_contract()
    {
        var resources = Directory
            .GetFiles(Path.Combine(AppContext.BaseDirectory, "Resources"), "Strings.*.json")
            .ToDictionary(
                path => Path.GetFileNameWithoutExtension(path)["Strings.".Length..],
                ReadResource,
                StringComparer.OrdinalIgnoreCase);

        Assert.Equal(Cultures, resources.Keys.Order(StringComparer.Ordinal).ToArray());
        var canonicalKeys = resources["en-US"].Keys.Order(StringComparer.Ordinal).ToArray();
        Assert.NotEmpty(canonicalKeys);

        foreach (var culture in Cultures)
        {
            Assert.Equal(canonicalKeys, resources[culture].Keys.Order(StringComparer.Ordinal).ToArray());
            Assert.All(resources[culture].Values, value => Assert.False(string.IsNullOrWhiteSpace(value)));
            Assert.Equal("GProxyT", resources[culture]["AppName"]);
        }

        Assert.DoesNotContain("your proxy", resources["en-US"]["LaunchFailed"], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("{0}", resources["en-US"]["ProxyUnavailable"], StringComparison.Ordinal);
        Assert.Contains("{0}", resources["en-US"]["CurrentProxy"], StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("MainWindow.xaml")]
    [InlineData("SettingsWindow.xaml")]
    public void Xaml_exposes_all_user_visible_text_through_localization_resources(string fileName)
    {
        var markup = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, fileName));

        Assert.DoesNotMatch(ChineseText(), markup);
        Assert.DoesNotContain("Title=\"gproxyt\"", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"Gproxyt\"", markup, StringComparison.Ordinal);
        Assert.Contains("AppLocalization.Current", markup, StringComparison.Ordinal);
        Assert.Contains("{Binding [", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Arabic_resources_request_right_to_left_layout()
    {
        Assert.Equal("RightToLeft", ReadResource(ResourcePath("ar-SA"))["FlowDirection"]);
        Assert.Equal("LeftToRight", ReadResource(ResourcePath("en-US"))["FlowDirection"]);
    }

    private static string ResourcePath(string culture) =>
        Path.Combine(AppContext.BaseDirectory, "Resources", $"Strings.{culture}.json");

    private static Dictionary<string, string> ReadResource(string path) =>
        JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path))
        ?? throw new InvalidDataException($"Localization resource is empty: {path}");

    [GeneratedRegex("[\\u3400-\\u9FFF]")]
    private static partial Regex ChineseText();
}
