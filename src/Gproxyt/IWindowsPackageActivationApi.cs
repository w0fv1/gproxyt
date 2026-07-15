namespace Gproxyt;

internal interface IWindowsPackageActivationApi
{
    void EnableDebugging(string packageFullName, IReadOnlyDictionary<string, string> environment);
    int ActivateApplication(string appUserModelId, string arguments);
    string? GetProcessPackageFamilyName(int processId);
    void DisableDebugging(string packageFullName);
}
