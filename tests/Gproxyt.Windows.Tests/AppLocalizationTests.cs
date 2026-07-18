using System.Globalization;

namespace Gproxyt.Windows.Tests;

public sealed class AppLocalizationTests
{
    [Fact]
    public void Loads_every_embedded_translation_into_the_localization_provider()
    {
        var embeddedCultures = typeof(AppLocalization).Assembly
            .GetManifestResourceNames()
            .Where(name => name.StartsWith("Gproxyt.Resources.Strings.", StringComparison.Ordinal)
                && name.EndsWith(".json", StringComparison.Ordinal))
            .Select(name => name["Gproxyt.Resources.Strings.".Length..^".json".Length])
            .Order(StringComparer.Ordinal)
            .ToArray();
        var supportedCultures = AppLocalization.SupportedCultures
            .Select(culture => culture.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(20, supportedCultures.Length);
        Assert.Equal(embeddedCultures, supportedCultures);

        var originalCulture = AppLocalization.Current.Culture;
        try
        {
            foreach (var culture in AppLocalization.SupportedCultures)
            {
                AppLocalization.ApplyCulture(culture);
                Assert.False(string.IsNullOrWhiteSpace(AppLocalization.Current["AppName"]));
            }
        }
        finally
        {
            AppLocalization.ApplyCulture(originalCulture);
        }
    }

    [Theory]
    [InlineData("de-AT", "de-DE")]
    [InlineData("zh-HK", "zh-TW")]
    [InlineData("zh-SG", "zh-CN")]
    [InlineData("pt-PT", "pt-BR")]
    [InlineData("sv-SE", "en-US")]
    public void Resolves_the_closest_supported_culture_with_english_fallback(string requested, string expected)
    {
        Assert.Equal(expected, AppLocalization.ResolveCulture(new CultureInfo(requested)).Name);
    }

    [Theory]
    [InlineData("ja-JP", "en-US", "ja-JP")]
    [InlineData(null, "zh-SG", "zh-CN")]
    [InlineData("invalid", "de-AT", "de-DE")]
    public void Resolves_a_saved_culture_with_system_fallback(string? saved, string system, string expected)
    {
        Assert.Equal(expected, AppLocalization.ResolveConfiguredCulture(saved, new CultureInfo(system)).Name);
    }

    [Fact]
    public void Applying_a_culture_notifies_existing_windows_and_replaces_their_strings()
    {
        var originalCulture = AppLocalization.Current.Culture;
        var changedProperties = new List<string?>();
        AppLocalization.Current.PropertyChanged += (_, eventArgs) => changedProperties.Add(eventArgs.PropertyName);

        try
        {
            AppLocalization.ApplyCulture(new CultureInfo("ja-JP"));

            Assert.Equal("ChatGPT を開く", AppLocalization.Current["LaunchChatGpt"]);
            Assert.Contains("Item[]", changedProperties);
        }
        finally
        {
            AppLocalization.ApplyCulture(originalCulture);
        }
    }
}
