namespace Gproxyt.Core;

public sealed class ChatGptInstallationLocator(IPackageRegistrationSource packageRegistrationSource) : IChatGptInstallationLocator
{
    public ChatGptInstallation Locate()
    {
        var registrations = packageRegistrationSource.FindCurrentUserRegistrations(ChatGptPackage.FamilyName);
        if (registrations.Count == 0)
        {
            throw new InvalidOperationException("未安装 Microsoft Store 版 ChatGPT。请先安装官方应用。");
        }
        if (registrations.Count != 1)
        {
            throw new InvalidOperationException("检测到多个当前用户 ChatGPT 主程序包，应用可能正在更新，请稍后重试。");
        }

        return ChatGptInstallation.FromRegistration(registrations[0]);
    }
}
