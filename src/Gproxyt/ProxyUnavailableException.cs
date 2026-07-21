using Gproxyt.Core;

namespace Gproxyt;

internal sealed class ProxyUnavailableException(ProxyEndpoint proxy) : Exception
{
    public ProxyEndpoint Proxy { get; } = proxy;
}
