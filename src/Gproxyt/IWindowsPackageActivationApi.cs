namespace Gproxyt;

internal interface IWindowsPackageActivationApi
{
    int ActivateApplication(string appUserModelId, string arguments);
    string? GetProcessPackageFamilyName(int processId);
    bool WaitForProcessExit(int processId, int milliseconds);
    void TerminateAllProcesses(string packageFullName);
    bool WaitForPackageExit(string packageFamilyName, int milliseconds);
    bool EnsureProcessWindowVisible(int processId, int milliseconds);
}
