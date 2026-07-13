namespace Gproxyt.Core;

public sealed record LaunchResult(ChatGptInstallation Installation, ProxyEndpoint Proxy, int StoppedProcessCount, int ProcessId);
