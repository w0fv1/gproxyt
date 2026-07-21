using Gproxyt.Core;

namespace Gproxyt.Windows.Tests;

public sealed class EnvironmentDiagnosticsTests
{
    [Fact]
    public async Task Unreachable_proxy_failure_preserves_the_configured_endpoint()
    {
        var proxy = ProxyEndpoint.Parse("http://127.0.0.1:1");
        var diagnostics = new EnvironmentDiagnostics(new ThrowingInstallationLocator());

        var exception = await Assert.ThrowsAsync<ProxyUnavailableException>(
            () => diagnostics.EnsureProxyReachableAsync(proxy));

        Assert.Equal(proxy, exception.Proxy);
    }

    private sealed class ThrowingInstallationLocator : IChatGptInstallationLocator
    {
        public ChatGptInstallation Locate() => throw new InvalidOperationException();
    }
}
