namespace Gproxyt.Core;

public sealed record LaunchResult(ChatGptInstallation Installation, ProxyEndpoint Proxy, int ProcessId);
