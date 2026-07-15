using Gproxyt.Core;

namespace Gproxyt;

internal sealed class WindowsPackageRegistrationSource(IApplicationLog? log = null) : IPackageRegistrationSource
{
    private readonly IApplicationLog log = log ?? ApplicationLog.None;

    public IReadOnlyList<PackageRegistration> FindCurrentUserRegistrations(string packageFamilyName)
    {
        this.log.Information("package_registration_query_started", ("PackageFamilyName", packageFamilyName));
        var registrations = WindowsPackageApi.FindCurrentUserRegistrations(packageFamilyName);
        this.log.Information(
            "package_registration_query_completed",
            ("PackageFamilyName", packageFamilyName),
            ("RegistrationCount", registrations.Count),
            ("PackageFullNames", registrations.Select(item => item.PackageFullName).ToArray()));
        return registrations;
    }
}
