using System.Collections.ObjectModel;

namespace Gproxyt.Core;

public sealed record ProxyLaunchPlan(
    IReadOnlyList<string> Arguments,
    IReadOnlyDictionary<string, string> Environment)
{
    private const string BypassHosts = "localhost,127.0.0.1,::1";
    private const string ChromiumBypassHosts = "<-loopback>;localhost;127.0.0.1;::1";

    public static ProxyLaunchPlan Create(ProxyEndpoint proxy)
    {
        ArgumentNullException.ThrowIfNull(proxy);

        var arguments = new[]
        {
            $"--proxy-server={proxy.Value}",
            $"--proxy-bypass-list={ChromiumBypassHosts}"
        };
        var environment = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["HTTP_PROXY"] = proxy.Value,
            ["HTTPS_PROXY"] = proxy.Value,
            ["ALL_PROXY"] = proxy.Value,
            ["NO_PROXY"] = BypassHosts,
            ["NODE_USE_ENV_PROXY"] = "1"
        };

        return new ProxyLaunchPlan(
            Array.AsReadOnly(arguments),
            new ReadOnlyDictionary<string, string>(environment));
    }
}
