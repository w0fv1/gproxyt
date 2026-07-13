namespace Gproxyt.Core;

public sealed record ProxyEndpoint
{
    private static readonly HashSet<string> SupportedSchemes = new(StringComparer.OrdinalIgnoreCase)
    {
        Uri.UriSchemeHttp,
        Uri.UriSchemeHttps,
        "socks5"
    };

    private ProxyEndpoint(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static ProxyEndpoint Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("代理地址不能为空。", nameof(value));
        }

        var normalized = value.Trim();
        if (!normalized.Contains("://", StringComparison.Ordinal))
        {
            normalized = $"http://{normalized}";
        }

        if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri) ||
            string.IsNullOrWhiteSpace(uri.Host) ||
            uri.Port <= 0 ||
            uri.AbsolutePath != "/" ||
            !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment) ||
            !SupportedSchemes.Contains(uri.Scheme))
        {
            throw new ArgumentException("代理地址必须是有效的 HTTP、HTTPS 或 SOCKS5 地址。", nameof(value));
        }

        return new ProxyEndpoint(uri.AbsoluteUri.TrimEnd('/'));
    }

    public override string ToString() => Value;
}
