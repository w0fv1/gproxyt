using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using Gproxyt.Core;
using Microsoft.Win32.SafeHandles;

namespace Gproxyt;

internal static class WindowsPackageApi
{
    private const uint PackageFilterHead = 0x00000010;
    private const uint ProcessPackageQueryAccess = 0x00100400;
    private const uint ProcessTerminateAccess = 0x00000001;
    private const int ErrorSuccess = 0;
    private const int ErrorInsufficientBuffer = 122;
    private const int AppmodelErrorNoPackage = 15700;
    private const uint WaitObject0 = 0;
    private const uint WaitTimeout = 258;
    private const uint WaitFailed = uint.MaxValue;

    public static IReadOnlyList<PackageRegistration> FindCurrentUserRegistrations(string packageFamilyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageFamilyName);
        uint count = 0;
        uint bufferLength = 0;
        var result = FindPackagesByPackageFamily(
            packageFamilyName,
            PackageFilterHead,
            ref count,
            IntPtr.Zero,
            ref bufferLength,
            IntPtr.Zero,
            IntPtr.Zero);
        if (result == ErrorSuccess && count == 0)
        {
            return [];
        }
        if (result != ErrorInsufficientBuffer || count == 0 || bufferLength == 0)
        {
            throw new Win32Exception(result, "无法查询当前用户的 ChatGPT 程序包。");
        }

        var names = Marshal.AllocHGlobal(checked((int)count * IntPtr.Size));
        try
        {
            var buffer = Marshal.AllocHGlobal(checked((int)bufferLength * sizeof(char)));
            try
            {
                result = FindPackagesByPackageFamily(
                    packageFamilyName,
                    PackageFilterHead,
                    ref count,
                    names,
                    ref bufferLength,
                    buffer,
                    IntPtr.Zero);
                if (result != ErrorSuccess)
                {
                    throw new Win32Exception(result, "查询当前用户的 ChatGPT 程序包时，程序包状态发生了变化。");
                }

                var registrations = new PackageRegistration[count];
                for (var index = 0; index < count; index++)
                {
                    var nameAddress = Marshal.ReadIntPtr(names, checked(index * IntPtr.Size));
                    var packageFullName = Marshal.PtrToStringUni(nameAddress)
                        ?? throw new InvalidOperationException("Windows 返回了无效的 ChatGPT 程序包名称。");
                    registrations[index] = new PackageRegistration(packageFullName, GetPackagePath(packageFullName));
                }
                return registrations;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(names);
        }
    }

    public static string? GetProcessPackageFamilyName(SafeProcessHandle process)
    {
        uint length = 0;
        var result = GetPackageFamilyName(process, ref length, null);
        if (result == AppmodelErrorNoPackage)
        {
            return null;
        }
        if (result != ErrorInsufficientBuffer || length == 0)
        {
            throw new Win32Exception(result, "无法读取进程的程序包身份。");
        }

        var familyName = new StringBuilder(checked((int)length));
        result = GetPackageFamilyName(process, ref length, familyName);
        if (result != ErrorSuccess)
        {
            throw new Win32Exception(result, "读取进程的程序包身份时，进程状态发生了变化。");
        }
        return familyName.ToString();
    }

    public static SafeProcessHandle OpenProcessForPackageQuery(int processId) =>
        OpenProcess(processId, ProcessPackageQueryAccess);

    public static SafeProcessHandle OpenProcessForTermination(int processId) =>
        OpenProcess(processId, ProcessTerminateAccess);

    private static SafeProcessHandle OpenProcess(int processId, uint access)
    {
        var process = OpenProcessNative(access, false, checked((uint)processId));
        if (!process.IsInvalid)
        {
            return process;
        }

        var error = Marshal.GetLastWin32Error();
        process.Dispose();
        throw new Win32Exception(error, $"无法打开进程 PID {processId}。");
    }

    public static int GetProcessSessionId(SafeProcessHandle process)
    {
        var processId = GetProcessId(process);
        if (processId == 0)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "无法读取进程 ID。");
        }
        if (!ProcessIdToSessionId(processId, out var sessionId))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "无法读取进程会话。");
        }
        return checked((int)sessionId);
    }

    public static void TerminateProcess(SafeProcessHandle process)
    {
        if (!TerminateProcessNative(process, uint.MaxValue))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "无法终止 ChatGPT 进程。");
        }
    }

    public static bool WaitForProcessExit(SafeProcessHandle process, int timeoutMilliseconds)
    {
        var result = WaitForSingleObject(process, checked((uint)timeoutMilliseconds));
        if (result == WaitObject0)
        {
            return true;
        }
        if (result == WaitTimeout)
        {
            return false;
        }
        if (result == WaitFailed)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "等待 ChatGPT 进程退出失败。");
        }
        throw new InvalidOperationException($"Windows 返回了未知的进程等待状态 {result}。");
    }

    private static string GetPackagePath(string packageFullName)
    {
        uint length = 0;
        var result = GetPackagePathByFullName(packageFullName, ref length, null);
        if (result != ErrorInsufficientBuffer || length == 0)
        {
            throw new Win32Exception(result, "ChatGPT 程序包正在安装或更新，请稍后重试。");
        }

        var path = new StringBuilder(checked((int)length));
        result = GetPackagePathByFullName(packageFullName, ref length, path);
        if (result != ErrorSuccess)
        {
            throw new Win32Exception(result, "ChatGPT 程序包正在安装或更新，请稍后重试。");
        }
        return path.ToString();
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int FindPackagesByPackageFamily(
        string packageFamilyName,
        uint packageFilters,
        ref uint count,
        IntPtr packageFullNames,
        ref uint bufferLength,
        IntPtr buffer,
        IntPtr packageProperties);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetPackagePathByFullName(
        string packageFullName,
        ref uint pathLength,
        StringBuilder? path);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetPackageFamilyName(
        SafeProcessHandle process,
        ref uint packageFamilyNameLength,
        StringBuilder? packageFamilyName);

    [DllImport("kernel32.dll", EntryPoint = "TerminateProcess", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool TerminateProcessNative(SafeProcessHandle process, uint exitCode);

    [DllImport("kernel32.dll", EntryPoint = "OpenProcess", SetLastError = true)]
    private static extern SafeProcessHandle OpenProcessNative(
        uint desiredAccess,
        [MarshalAs(UnmanagedType.Bool)] bool inheritHandle,
        uint processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint GetProcessId(SafeProcessHandle process);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ProcessIdToSessionId(uint processId, out uint sessionId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint WaitForSingleObject(SafeProcessHandle handle, uint milliseconds);
}
