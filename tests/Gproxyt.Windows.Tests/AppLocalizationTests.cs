using System.Globalization;
using Lepo.i18n;

namespace Gproxyt.Windows.Tests;

public sealed class AppLocalizationTests
{
    [Fact]
    public void Loads_every_embedded_translation_into_the_localization_provider()
    {
        var builder = new LocalizationBuilder();

        AppLocalization.Configure(builder, new CultureInfo("zh-CN"));
        var provider = builder.Build();

        Assert.Equal("zh-CN", provider.GetCulture().Name);
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
}
