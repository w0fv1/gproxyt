using System.Globalization;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;

namespace Gproxyt;

public sealed class AppLocalization : INotifyPropertyChanged
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
    private static readonly AppLocalization current = Load(CulturesByName["en-US"]);
    private IReadOnlyDictionary<string, string> strings;

    private AppLocalization(CultureInfo culture, IReadOnlyDictionary<string, string> strings)
    {
        Culture = culture;
        this.strings = strings;
    }

    public static AppLocalization Current => current;

    public event PropertyChangedEventHandler? PropertyChanged;

    public CultureInfo Culture { get; private set; }

    public FlowDirection FlowDirection => Culture.TextInfo.IsRightToLeft
        ? FlowDirection.RightToLeft
        : FlowDirection.LeftToRight;

    public string this[string key] => strings.TryGetValue(key, out var value)
        ? value
        : throw new KeyNotFoundException($"Missing localization key '{key}' for {Culture.Name}.");

    internal static void Initialize(CultureInfo requestedCulture) => ApplyCulture(requestedCulture);

    internal static void ApplyCulture(CultureInfo culture)
    {
        var localized = Load(ResolveCulture(culture));
        current.Culture = localized.Culture;
        current.strings = localized.strings;
        CultureInfo.CurrentUICulture = current.Culture;
        current.PropertyChanged?.Invoke(current, new PropertyChangedEventArgs("Item[]"));
        current.PropertyChanged?.Invoke(current, new PropertyChangedEventArgs(nameof(Culture)));
        current.PropertyChanged?.Invoke(current, new PropertyChangedEventArgs(nameof(FlowDirection)));
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

    internal static CultureInfo ResolveConfiguredCulture(string? cultureName, CultureInfo systemCulture)
    {
        if (!string.IsNullOrWhiteSpace(cultureName) && CulturesByName.TryGetValue(cultureName, out var configured))
        {
            return configured;
        }
        return ResolveCulture(systemCulture);
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
