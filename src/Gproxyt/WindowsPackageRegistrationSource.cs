using Gproxyt.Core;

namespace Gproxyt;

internal sealed class WindowsPackageRegistrationSource : IPackageRegistrationSource
{
    public IReadOnlyList<PackageRegistration> FindCurrentUserRegistrations(string packageFamilyName) =>
        WindowsPackageApi.FindCurrentUserRegistrations(packageFamilyName);
}
