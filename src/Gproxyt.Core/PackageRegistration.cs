namespace Gproxyt.Core;

public sealed record PackageRegistration
{
    public PackageRegistration(string packageFullName, string installLocation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageFullName);
        ArgumentException.ThrowIfNullOrWhiteSpace(installLocation);
        PackageFullName = packageFullName.Trim();
        InstallLocation = Path.GetFullPath(installLocation.Trim());
    }

    public string PackageFullName { get; }
    public string InstallLocation { get; }
}
