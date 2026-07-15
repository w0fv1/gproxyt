using Gproxyt.Core;

namespace Gproxyt.Core.Tests;

public sealed class ProxyLaunchPlanTests
{
    [Fact]
    public void Create_configures_chromium_proxy_settings()
    {
        var plan = ProxyLaunchPlan.Create(ProxyEndpoint.Parse("127.0.0.1:7890"));

        Assert.Contains("--proxy-server=http://127.0.0.1:7890", plan.Arguments);
        Assert.Contains("--proxy-bypass-list=<-loopback>;localhost;127.0.0.1;::1", plan.Arguments);
        Assert.Equal(2, plan.Arguments.Count);
    }
}
