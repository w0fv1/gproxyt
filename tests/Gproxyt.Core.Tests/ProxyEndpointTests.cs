using Gproxyt.Core;

namespace Gproxyt.Core.Tests;

public sealed class ProxyEndpointTests
{
    [Theory]
    [InlineData("127.0.0.1:7890", "http://127.0.0.1:7890")]
    [InlineData(" HTTP://127.0.0.1:7890/ ", "http://127.0.0.1:7890")]
    [InlineData("socks5://localhost:7891", "socks5://localhost:7891")]
    public void Parse_normalizes_supported_proxy_addresses(string value, string expected)
    {
        Assert.Equal(expected, ProxyEndpoint.Parse(value).Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ftp://127.0.0.1:7890")]
    [InlineData("http://")]
    [InlineData("http://127.0.0.1:7890/path")]
    [InlineData("socks5://127.0.0.1")]
    public void Parse_rejects_invalid_proxy_addresses(string value)
    {
        Assert.Throws<ArgumentException>(() => ProxyEndpoint.Parse(value));
    }
}
