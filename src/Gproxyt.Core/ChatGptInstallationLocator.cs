namespace Gproxyt.Core;

public sealed class ChatGptInstallationLocator(IPackageLocationSource packageLocationSource) : IChatGptInstallationLocator
{
    public ChatGptInstallation Locate()
    {
        var installLocation = packageLocationSource.GetLatestInstallLocation();
        if (string.IsNullOrWhiteSpace(installLocation))
        {
            throw new InvalidOperationException("未安装 Microsoft Store 版 ChatGPT。请先安装官方应用。");
        }

        return ChatGptInstallation.FromInstallLocation(installLocation);
    }
}
