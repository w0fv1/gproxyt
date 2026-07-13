namespace Gproxyt.Core;

public interface IPackageLocationSource
{
    string? GetLatestInstallLocation();
}
