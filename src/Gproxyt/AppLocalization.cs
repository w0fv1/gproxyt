using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using Lepo.i18n;
using Lepo.i18n.Wpf;

namespace Gproxyt;

internal sealed class AppLocalization
{
    internal static readonly CultureInfo[] SupportedCultures =
    [
        new("ar-SA"), new("de-DE"), new("en-US"), new("es-ES"), new("fr-FR"), new("hi-IN"),
        new("id-ID"), new("it-IT"), new("ja-JP"), new("ko-KR"), new("nl-NL"), new("pl-PL"),
        new("pt-BR"), new("ru-RU"), new("th-TH"), new("tr-TR"), new("uk-UA"), new("vi-VN"),
        new("zh-CN"), new("zh-TW")
    ];

    private static readonly Assembly Assembly = typeof(AppLocalization).Assembly;
    private static readonly Dictionary<string, CultureInfo> CulturesByName = SupportedCultures
        .ToDictionary(culture => culture.Name, StringComparer.OrdinalIgnoreCase);
    private static AppLocalization current = Load(CulturesByName["en-US"]);
    private readonly IReadOnlyDictionary<string, string> strings;

    private AppLocalization(CultureInfo culture, IReadOnlyDictionary<string, string> strings)
    {
        Culture = culture;
        this.strings = strings;
    }

    internal static AppLocalization Current => current;

    internal CultureInfo Culture { get; }

    internal FlowDirection FlowDirection => Culture.TextInfo.IsRightToLeft
        ? FlowDirection.RightToLeft
        : FlowDirection.LeftToRight;

    internal string this[string key] => strings.TryGetValue(key, out var value)
        ? value
        : throw new KeyNotFoundException($"Missing localization key '{key}' for {Culture.Name}.");

    internal static void Initialize(App application, CultureInfo requestedCulture)
    {
        current = Load(ResolveCulture(requestedCulture));
        CultureInfo.CurrentUICulture = current.Culture;
        application.UseStringLocalizer(builder => Configure(builder, current.Culture));
    }

    internal static void Configure(LocalizationBuilder builder, CultureInfo culture)
    {
        builder.SetCulture(culture);
        foreach (var supportedCulture in SupportedCultures)
        {
            builder.AddLocalization(
                supportedCulture,
                Load(supportedCulture).strings.Select(value =>
                    new KeyValuePair<string, string?>(value.Key, value.Value)));
        }
    }

    internal static CultureInfo ResolveCulture(CultureInfo requestedCulture)
    {
        if (CulturesByName.TryGetValue(requestedCulture.Name, out var exact))
        {
            return exact;
        }

        if (requestedCulture.TwoLetterISOLanguageName.Equals("zh", StringComparison.OrdinalIgnoreCase))
        {
            var traditional = requestedCulture.Name.Contains("Hant", StringComparison.OrdinalIgnoreCase)
                || requestedCulture.Name.EndsWith("-HK", StringComparison.OrdinalIgnoreCase)
                || requestedCulture.Name.EndsWith("-MO", StringComparison.OrdinalIgnoreCase)
                || requestedCulture.Name.EndsWith("-TW", StringComparison.OrdinalIgnoreCase);
            return CulturesByName[traditional ? "zh-TW" : "zh-CN"];
        }

        var languageMatch = SupportedCultures.FirstOrDefault(culture =>
            culture.TwoLetterISOLanguageName.Equals(requestedCulture.TwoLetterISOLanguageName, StringComparison.OrdinalIgnoreCase));
        return languageMatch ?? CulturesByName["en-US"];
    }

    private static AppLocalization Load(CultureInfo culture)
    {
        using var stream = Assembly.GetManifestResourceStream(ResourceName(culture))
            ?? throw new InvalidOperationException($"Missing localization resource for {culture.Name}.");
        var values = JsonSerializer.Deserialize<Dictionary<string, string>>(stream)
            ?? throw new InvalidDataException($"Localization resource is empty for {culture.Name}.");
        return new AppLocalization(culture, values);
    }

    private static string ResourceName(CultureInfo culture) =>
        $"Gproxyt.Resources.Strings.{culture.Name}.json";
}
