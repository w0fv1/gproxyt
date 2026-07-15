namespace Gproxyt.Core;

public interface IPackageRegistrationSource
{
    IReadOnlyList<PackageRegistration> FindCurrentUserRegistrations(string packageFamilyName);
}
