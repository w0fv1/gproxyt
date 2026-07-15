namespace Gproxyt.Core;

public interface IProcessManager
{
    void Stop(ChatGptInstallation installation);
    int Start(ChatGptInstallation installation, ProxyLaunchPlan plan);
}
