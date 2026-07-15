namespace Gproxyt.Core;

public sealed class PackageProcessScope
{
    private readonly string packageFamilyName;

    public PackageProcessScope(string packageFamilyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageFamilyName);
        this.packageFamilyName = packageFamilyName;
    }

    public bool Contains(string? candidate) =>
        string.Equals(packageFamilyName, candidate, StringComparison.OrdinalIgnoreCase);
}
