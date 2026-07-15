namespace Gproxyt.Core;

public sealed record ProxyLaunchPlan(IReadOnlyList<string> Arguments)
{
    private const string ChromiumBypassHosts = "<-loopback>;localhost;127.0.0.1;::1";

    public static ProxyLaunchPlan Create(ProxyEndpoint proxy)
    {
        ArgumentNullException.ThrowIfNull(proxy);

        var arguments = new[]
        {
            $"--proxy-server={proxy.Value}",
            $"--proxy-bypass-list={ChromiumBypassHosts}"
        };
        return new ProxyLaunchPlan(Array.AsReadOnly(arguments));
    }
}
