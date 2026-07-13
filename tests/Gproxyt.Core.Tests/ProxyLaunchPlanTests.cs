using Gproxyt.Core;

namespace Gproxyt.Core.Tests;

public sealed class ProxyLaunchPlanTests
{
    [Fact]
    public void Create_configures_chromium_and_process_tree_proxy_settings()
    {
        var executable = @"C:\Program Files\WindowsApps\OpenAI.Codex_1\app\ChatGPT.exe";
        var plan = ProxyLaunchPlan.Create(executable, ProxyEndpoint.Parse("127.0.0.1:7890"));

        Assert.Equal(executable, plan.ExecutablePath);
        Assert.Equal(Path.GetDirectoryName(executable), plan.WorkingDirectory);
        Assert.Contains("--proxy-server=http://127.0.0.1:7890", plan.Arguments);
        Assert.Contains("--proxy-bypass-list=<-loopback>;localhost;127.0.0.1;::1", plan.Arguments);
        Assert.Equal("http://127.0.0.1:7890", plan.Environment["HTTP_PROXY"]);
        Assert.Equal("http://127.0.0.1:7890", plan.Environment["HTTPS_PROXY"]);
        Assert.Equal("http://127.0.0.1:7890", plan.Environment["ALL_PROXY"]);
        Assert.Equal("localhost,127.0.0.1,::1", plan.Environment["NO_PROXY"]);
        Assert.Equal("1", plan.Environment["NODE_USE_ENV_PROXY"]);
    }
}
