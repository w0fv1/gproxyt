namespace Gproxyt;

internal static class StartupRegistration
{
    public static IStartupRegistration Create() =>
        PackageIdentity.IsPackaged
            ? new PackagedStartupRegistration()
            : new WindowsStartupRegistration();
}
