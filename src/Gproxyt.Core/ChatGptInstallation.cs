namespace Gproxyt.Core;

public sealed record ChatGptInstallation(
    string PackageFullName,
    string PackageFamilyName,
    string AppUserModelId,
    string InstallLocation,
    string ExecutablePath)
{
    public static ChatGptInstallation FromRegistration(PackageRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(registration);

        var executablePath = Path.GetFullPath(Path.Combine(registration.InstallLocation, ChatGptPackage.RelativeExecutablePath));
        if (!File.Exists(executablePath))
        {
            throw new FileNotFoundException("没有在 Microsoft Store 包中找到 ChatGPT.exe。", executablePath);
        }

        return new ChatGptInstallation(
            registration.PackageFullName,
            ChatGptPackage.FamilyName,
            ChatGptPackage.AppUserModelId,
            registration.InstallLocation,
            executablePath);
    }
}
