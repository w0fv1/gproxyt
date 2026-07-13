using System.Collections.ObjectModel;

namespace Gproxyt.Core;

public sealed record ProxyLaunchPlan(
    string ExecutablePath,
    string WorkingDirectory,
    IReadOnlyList<string> Arguments,
    IReadOnlyDictionary<string, string> Environment)
{
    private const string BypassHosts = "localhost,127.0.0.1,::1";
    private const string ChromiumBypassHosts = "<-loopback>;localhost;127.0.0.1;::1";

    public static ProxyLaunchPlan Create(string executablePath, ProxyEndpoint proxy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);
        ArgumentNullException.ThrowIfNull(proxy);

        var fullPath = Path.GetFullPath(executablePath);
        var workingDirectory = Path.GetDirectoryName(fullPath) ?? throw new ArgumentException("应用路径缺少工作目录。", nameof(executablePath));
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
            ["http_proxy"] = proxy.Value,
            ["https_proxy"] = proxy.Value,
            ["all_proxy"] = proxy.Value,
            ["NO_PROXY"] = BypassHosts,
            ["no_proxy"] = BypassHosts,
            ["NODE_USE_ENV_PROXY"] = "1"
        };

        return new ProxyLaunchPlan(
            fullPath,
            workingDirectory,
            Array.AsReadOnly(arguments),
            new ReadOnlyDictionary<string, string>(environment));
    }
}
