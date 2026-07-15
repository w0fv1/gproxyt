using System.ComponentModel;
using Gproxyt.Core;

namespace Gproxyt;

internal sealed class WindowsProcessManager : IProcessManager
{
    private const int StartupStabilityMilliseconds = 1500;
    private const int PackageTerminationTimeoutMilliseconds = 10000;
    private const int WindowVisibilityTimeoutMilliseconds = 10000;
    private readonly IWindowsPackageActivationApi activationApi;
    private readonly IApplicationLog log;

    public WindowsProcessManager() : this(new WindowsPackageActivationApi(), ApplicationLog.None)
    {
    }

    internal WindowsProcessManager(IApplicationLog log) : this(new WindowsPackageActivationApi(), log)
    {
    }

    internal WindowsProcessManager(IWindowsPackageActivationApi activationApi)
        : this(activationApi, ApplicationLog.None)
    {
    }

    internal WindowsProcessManager(IWindowsPackageActivationApi activationApi, IApplicationLog log)
    {
        this.activationApi = activationApi;
        this.log = log;
    }

    public void Stop(ChatGptInstallation installation)
    {
        ArgumentNullException.ThrowIfNull(installation);
        try
        {
            log.Information("package_termination_started", ("PackageFullName", installation.PackageFullName));
            activationApi.TerminateAllProcesses(installation.PackageFullName);
            if (!activationApi.WaitForPackageExit(
                installation.PackageFamilyName,
                PackageTerminationTimeoutMilliseconds))
            {
                throw new InvalidOperationException("等待现有 ChatGPT 进程退出超时。");
            }
            log.Information("package_termination_completed", ("PackageFullName", installation.PackageFullName));
        }
        catch (Exception exception) when (
            exception is Win32Exception or InvalidOperationException or System.Runtime.InteropServices.COMException)
        {
            throw new InvalidOperationException($"无法关闭现有 ChatGPT 进程：{exception.Message}", exception);
        }
    }

    public int Start(ChatGptInstallation installation, ProxyLaunchPlan plan)
    {
        ArgumentNullException.ThrowIfNull(installation);
        ArgumentNullException.ThrowIfNull(plan);
        try
        {
            log.Information(
                "package_activation_started",
                ("PackageFullName", installation.PackageFullName),
                ("AppUserModelId", installation.AppUserModelId),
                ("ArgumentCount", plan.Arguments.Count));
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
            log.Information(
                "package_identity_verified",
                ("ProcessId", processId),
                ("PackageFamilyName", packageFamilyName));
            if (activationApi.WaitForProcessExit(processId, StartupStabilityMilliseconds))
            {
                throw new InvalidOperationException("ChatGPT 启动后立即退出。");
            }
            log.Information(
                "package_activation_stable",
                ("ProcessId", processId),
                ("StabilityMilliseconds", StartupStabilityMilliseconds));
            if (!activationApi.EnsureProcessWindowVisible(processId, WindowVisibilityTimeoutMilliseconds))
            {
                throw new InvalidOperationException("ChatGPT 已启动，但没有创建可见窗口。");
            }
            log.Information("package_window_visible", ("ProcessId", processId));
            return processId;
        }
        catch (Exception exception) when (
            exception is Win32Exception or InvalidOperationException or System.Runtime.InteropServices.COMException)
        {
            throw new PackageLaunchTargetUnavailableException($"无法启动 ChatGPT：{exception.Message}", exception);
        }
    }
}
