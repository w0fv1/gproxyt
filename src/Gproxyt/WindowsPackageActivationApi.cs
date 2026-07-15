using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Gproxyt;

internal sealed class WindowsPackageActivationApi : IWindowsPackageActivationApi
{
    private const uint ActivateOptionsNoErrorUi = 0x00000002;
    private const int ShowWindowRestore = 9;
    private static readonly Guid PackageDebugSettingsClassId = new("B1AEC16F-2383-4852-B0E9-8F0B1DC66B4D");
    private static readonly Guid ApplicationActivationManagerClassId = new("45BA127D-10A8-46EA-8AB7-56EA9078943C");

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

    public bool WaitForProcessExit(int processId, int milliseconds)
    {
        using var handle = WindowsPackageApi.OpenProcessForPackageQuery(processId);
        return WindowsPackageApi.WaitForProcessExit(handle, milliseconds);
    }

    public void TerminateAllProcesses(string packageFullName)
    {
        var settings = CreateComInstance<IPackageDebugSettings>(PackageDebugSettingsClassId);
        try
        {
            Marshal.ThrowExceptionForHR(settings.TerminateAllProcesses(packageFullName));
        }
        finally
        {
            Marshal.FinalReleaseComObject(settings);
        }
    }

    public bool WaitForPackageExit(string packageFamilyName, int milliseconds)
    {
        var elapsed = Stopwatch.StartNew();
        while (elapsed.ElapsedMilliseconds < milliseconds)
        {
            if (!IsPackageRunning(packageFamilyName))
            {
                return true;
            }
            Thread.Sleep(50);
        }
        return !IsPackageRunning(packageFamilyName);
    }

    public bool EnsureProcessWindowVisible(int processId, int milliseconds)
    {
        var elapsed = Stopwatch.StartNew();
        while (elapsed.ElapsedMilliseconds < milliseconds)
        {
            var window = FindApplicationWindow(processId);
            if (window != IntPtr.Zero)
            {
                if (!IsWindowVisible(window))
                {
                    ShowWindowAsync(window, ShowWindowRestore);
                }
                SetForegroundWindow(window);
                if (IsWindowVisible(window))
                {
                    return true;
                }
            }
            Thread.Sleep(50);
        }
        return false;
    }

    private static bool IsPackageRunning(string packageFamilyName)
    {
        foreach (var process in Process.GetProcesses())
        {
            using (process)
            {
                try
                {
                    using var handle = WindowsPackageApi.OpenProcessForPackageQuery(process.Id);
                    if (string.Equals(
                        packageFamilyName,
                        WindowsPackageApi.GetProcessPackageFamilyName(handle),
                        StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                catch (Win32Exception exception) when (exception.NativeErrorCode is 5 or 6 or 87)
                {
                }
            }
        }
        return false;
    }

    private static IntPtr FindApplicationWindow(int processId)
    {
        var result = IntPtr.Zero;
        EnumWindows((window, _) =>
        {
            GetWindowThreadProcessId(window, out var ownerProcessId);
            if (ownerProcessId != processId)
            {
                return true;
            }
            var title = new StringBuilder(256);
            GetWindowText(window, title, title.Capacity);
            if (title.Length == 0)
            {
                return true;
            }
            result = window;
            return false;
        }, IntPtr.Zero);
        return result;
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

        [PreserveSig]
        int Suspend([MarshalAs(UnmanagedType.LPWStr)] string packageFullName);

        [PreserveSig]
        int Resume([MarshalAs(UnmanagedType.LPWStr)] string packageFullName);

        [PreserveSig]
        int TerminateAllProcesses([MarshalAs(UnmanagedType.LPWStr)] string packageFullName);
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

    private delegate bool EnumWindowsCallback(IntPtr window, IntPtr parameter);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumWindows(EnumWindowsCallback callback, IntPtr parameter);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr window, out int processId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr window, StringBuilder text, int maximumCount);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr window);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindowAsync(IntPtr window, int command);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr window);
}
