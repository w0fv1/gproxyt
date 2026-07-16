using System.Globalization;

namespace Gproxyt.Windows.Tests;

public sealed class AppLocalizationTests
{
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
}
