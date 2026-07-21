using System.Net.Sockets;
using Gproxyt.Core;
using Microsoft.Win32;

namespace Gproxyt;

internal sealed record DiagnosticSnapshot(
    bool SystemProxyEnabled,
    bool ProxyReachable,
    ChatGptInstallation? Installation,
    string? Error);

internal sealed class EnvironmentDiagnostics(IChatGptInstallationLocator installationLocator)
{
    public async Task<DiagnosticSnapshot> InspectAsync(ProxyEndpoint proxy)
    {
        ChatGptInstallation? installation = null;
        string? error = null;
        try
        {
            installation = installationLocator.Locate();
        }
        catch (Exception exception)
        {
            error = exception.Message;
        }

        var proxyReachable = await CanConnectAsync(proxy);
        return new DiagnosticSnapshot(IsSystemProxyEnabled(), proxyReachable, installation, error);
    }

    public async Task EnsureProxyReachableAsync(ProxyEndpoint proxy)
    {
        if (!await CanConnectAsync(proxy))
        {
            throw new ProxyUnavailableException(proxy);
        }
    }

    private static async Task<bool> CanConnectAsync(ProxyEndpoint proxy)
    {
        var uri = new Uri(proxy.Value);
        using var client = new TcpClient();
        try
        {
            await client.ConnectAsync(uri.Host, uri.Port).WaitAsync(TimeSpan.FromSeconds(2));
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsSystemProxyEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Internet Settings");
        return key?.GetValue("ProxyEnable") is int value && value != 0;
    }
}
