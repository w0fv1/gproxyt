using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Gproxyt;

internal static class PackageIdentity
{
    private const int AppModelErrorNoPackage = 15700;
    private const int ErrorInsufficientBuffer = 122;

    public static bool IsPackaged
    {
        get
        {
            uint length = 0;
            var result = GetCurrentPackageFullName(ref length, null);
            return result switch
            {
                ErrorInsufficientBuffer => true,
                AppModelErrorNoPackage => false,
                _ => throw new Win32Exception(result)
            };
        }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetCurrentPackageFullName(ref uint packageFullNameLength, char[]? packageFullName);
}
