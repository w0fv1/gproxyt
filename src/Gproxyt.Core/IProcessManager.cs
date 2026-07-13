namespace Gproxyt.Core;

public interface IProcessManager
{
    int Stop(ProcessScope scope);
    int Start(ProxyLaunchPlan plan);
}
