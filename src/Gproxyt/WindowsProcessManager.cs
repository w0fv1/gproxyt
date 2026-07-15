using System.ComponentModel;
using System.Diagnostics;
using Gproxyt.Core;
using Microsoft.Win32.SafeHandles;

namespace Gproxyt;

internal sealed class WindowsProcessManager : IProcessManager
{
    private const int StopTimeoutMilliseconds = 10000;
    private readonly IWindowsPackageActivationApi activationApi;

    public WindowsProcessManager() : this(new WindowsPackageActivationApi())
    {
    }

    internal WindowsProcessManager(IWindowsPackageActivationApi activationApi)
    {
        this.activationApi = activationApi;
    }

    public int Stop(PackageProcessScope scope)
    {
        using var currentProcess = Process.GetCurrentProcess();
        var currentSessionId = currentProcess.SessionId;
        var elapsed = Stopwatch.StartNew();
        var stoppedProcessIds = new HashSet<int>();

        while (true)
        {
            var targets = new List<(SafeProcessHandle Handle, int Id)>();
            try
            {
                foreach (var process in Process.GetProcesses())
                {
                    using (process)
                    {
                        SafeProcessHandle? handle = null;
                        try
                        {
                            handle = WindowsPackageApi.OpenProcessForPackageQuery(process.Id);
                            if (WindowsPackageApi.GetProcessSessionId(handle) == currentSessionId
                                && scope.Contains(WindowsPackageApi.GetProcessPackageFamilyName(handle)))
                            {
                                targets.Add((handle, process.Id));
                                handle = null;
                            }
                        }
                        catch (Win32Exception exception) when (exception.NativeErrorCode is 5 or 6 or 87)
                        {
                        }
                        finally
                        {
                            handle?.Dispose();
                        }
                    }
                }
            }
            catch
            {
                foreach (var target in targets)
                {
                    target.Handle.Dispose();
                }
                throw;
            }

            if (targets.Count == 0)
            {
                return stoppedProcessIds.Count;
            }

            var failures = new List<string>();
            try
            {
                foreach (var target in targets)
                {
                    try
                    {
                        if (!WindowsPackageApi.WaitForProcessExit(target.Handle, 0))
                        {
                            try
                            {
                                using var terminationHandle = WindowsPackageApi.OpenProcessForTermination(target.Id);
                                WindowsPackageApi.TerminateProcess(terminationHandle);
                            }
                            catch (Win32Exception exception) when (
                                exception.NativeErrorCode is 5 or 6 or 87
                                && WindowsPackageApi.WaitForProcessExit(target.Handle, 0))
                            {
                            }
                        }
                        var remaining = checked((int)Math.Max(0, StopTimeoutMilliseconds - elapsed.ElapsedMilliseconds));
                        if (!WindowsPackageApi.WaitForProcessExit(target.Handle, remaining))
                        {
                            throw new TimeoutException("等待进程退出超时。");
                        }
                        stoppedProcessIds.Add(target.Id);
                    }
                    catch (Exception exception) when (
                        exception is Win32Exception or TimeoutException)
                    {
                        failures.Add($"PID {target.Id}: {exception.Message}");
                    }
                }
            }
            finally
            {
                foreach (var target in targets)
                {
                    target.Handle.Dispose();
                }
            }

            if (failures.Count > 0)
            {
                throw new InvalidOperationException($"无法关闭现有 ChatGPT 进程：{string.Join("；", failures)}");
            }
        }
    }

    public int Start(ChatGptInstallation installation, ProxyLaunchPlan plan)
    {
        ArgumentNullException.ThrowIfNull(installation);
        ArgumentNullException.ThrowIfNull(plan);
        var debuggingEnabled = false;
        try
        {
            activationApi.EnableDebugging(installation.PackageFullName, plan.Environment);
            debuggingEnabled = true;
            var processId = activationApi.ActivateApplication(
                installation.AppUserModelId,
                string.Join(' ', plan.Arguments));
            var packageFamilyName = activationApi.GetProcessPackageFamilyName(processId);
            if (!string.Equals(
                installation.PackageFamilyName,
                packageFamilyName,
                StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Windows 激活的进程不属于预期的 ChatGPT 程序包身份。");
            }
            return processId;
        }
        catch (Exception exception) when (
            exception is Win32Exception or InvalidOperationException or System.Runtime.InteropServices.COMException)
        {
            throw new PackageLaunchTargetUnavailableException($"无法启动 ChatGPT：{exception.Message}", exception);
        }
        finally
        {
            if (debuggingEnabled)
            {
                try
                {
                    activationApi.DisableDebugging(installation.PackageFullName);
                }
                catch (Exception exception) when (
                    exception is Win32Exception or InvalidOperationException or System.Runtime.InteropServices.COMException)
                {
                    throw new InvalidOperationException("无法恢复 ChatGPT 程序包状态，请重新启动 Windows 后再试。", exception);
                }
            }
        }
    }
}
