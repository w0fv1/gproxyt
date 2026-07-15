namespace Gproxyt.Core;

public interface IProcessManager
{
    int Stop(PackageProcessScope scope);
    int Start(ChatGptInstallation installation, ProxyLaunchPlan plan);
}
