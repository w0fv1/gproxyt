using System.Runtime.InteropServices;

namespace Gproxyt;

internal sealed class WindowsPackageActivationApi : IWindowsPackageActivationApi
{
    private const uint ActivateOptionsNoErrorUi = 0x00000002;
    private static readonly Guid PackageDebugSettingsClassId = new("B1AEC16F-2383-4852-B0E9-8F0B1DC66B4D");
    private static readonly Guid ApplicationActivationManagerClassId = new("45BA127D-10A8-46EA-8AB7-56EA9078943C");

    public void EnableDebugging(string packageFullName, IReadOnlyDictionary<string, string> environment)
    {
        using var environmentBlock = new WindowsEnvironmentBlock(environment);
        var executablePath = Environment.ProcessPath
            ?? throw new InvalidOperationException("无法确定 gproxyt 可执行文件位置。");
        if (executablePath.Contains('"'))
        {
            throw new InvalidOperationException("gproxyt 可执行文件路径包含 Windows 不支持的字符。");
        }
        var debuggerCommandLine = $"\"{executablePath}\" --package-debugger";
        var settings = CreateComInstance<IPackageDebugSettings>(PackageDebugSettingsClassId);
        try
        {
            Marshal.ThrowExceptionForHR(settings.EnableDebugging(packageFullName, debuggerCommandLine, environmentBlock.Pointer));
        }
        finally
        {
            Marshal.FinalReleaseComObject(settings);
        }
    }

    public int ActivateApplication(string appUserModelId, string arguments)
    {
        var manager = CreateComInstance<IApplicationActivationManager>(ApplicationActivationManagerClassId);
        try
        {
            Marshal.ThrowExceptionForHR(manager.ActivateApplication(
                appUserModelId,
                arguments,
                ActivateOptionsNoErrorUi,
                out var processId));
            return checked((int)processId);
        }
        finally
        {
            Marshal.FinalReleaseComObject(manager);
        }
    }

    public string? GetProcessPackageFamilyName(int processId)
    {
        using var handle = WindowsPackageApi.OpenProcessForPackageQuery(processId);
        return WindowsPackageApi.GetProcessPackageFamilyName(handle);
    }

    public void DisableDebugging(string packageFullName)
    {
        var settings = CreateComInstance<IPackageDebugSettings>(PackageDebugSettingsClassId);
        try
        {
            Marshal.ThrowExceptionForHR(settings.DisableDebugging(packageFullName));
        }
        finally
        {
            Marshal.FinalReleaseComObject(settings);
        }
    }

    private static T CreateComInstance<T>(Guid classId)
    {
        var type = Type.GetTypeFromCLSID(classId, true)
            ?? throw new InvalidOperationException("Windows 程序包激活服务不可用。");
        return (T)(Activator.CreateInstance(type)
            ?? throw new InvalidOperationException("无法创建 Windows 程序包激活服务。"));
    }

    [ComImport]
    [Guid("F27C3930-8029-4AD1-94E3-3DBA417810C1")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPackageDebugSettings
    {
        [PreserveSig]
        int EnableDebugging(
            [MarshalAs(UnmanagedType.LPWStr)] string packageFullName,
            [MarshalAs(UnmanagedType.LPWStr)] string? debuggerCommandLine,
            IntPtr environment);

        [PreserveSig]
        int DisableDebugging([MarshalAs(UnmanagedType.LPWStr)] string packageFullName);
    }

    [ComImport]
    [Guid("2E941141-7F97-4756-BA1D-9DECDE894A3D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IApplicationActivationManager
    {
        [PreserveSig]
        int ActivateApplication(
            [MarshalAs(UnmanagedType.LPWStr)] string appUserModelId,
            [MarshalAs(UnmanagedType.LPWStr)] string arguments,
            uint options,
            out uint processId);
    }
}
