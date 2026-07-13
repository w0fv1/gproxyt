using System.Diagnostics;
using Gproxyt.Core;

namespace Gproxyt;

internal sealed class WindowsProcessManager : IProcessManager
{
    public int Stop(ProcessScope scope)
    {
        var targets = new List<Process>();
        foreach (var process in Process.GetProcesses())
        {
            if (scope.Contains(GetExecutablePath(process)))
            {
                targets.Add(process);
            }
            else
            {
                process.Dispose();
            }
        }
        var stopped = 0;
        var failures = new List<string>();

        foreach (var process in targets)
        {
            using (process)
            {
                try
                {
                    process.Kill(true);
                    process.WaitForExit(5000);
                    stopped++;
                }
                catch (InvalidOperationException)
                {
                    stopped++;
                }
                catch (Exception exception)
                {
                    failures.Add($"{process.ProcessName} ({process.Id}): {exception.Message}");
                }
            }
        }

        if (failures.Count > 0)
        {
            throw new InvalidOperationException($"无法关闭现有 ChatGPT 进程：{string.Join("；", failures)}");
        }

        return stopped;
    }

    public int Start(ProxyLaunchPlan plan)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = plan.ExecutablePath,
            WorkingDirectory = plan.WorkingDirectory,
            UseShellExecute = false
        };
        foreach (var argument in plan.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }
        foreach (var variable in plan.Environment)
        {
            startInfo.Environment[variable.Key] = variable.Value;
        }

        var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Windows 没有返回 ChatGPT 进程。");
        return process.Id;
    }

    private static string? GetExecutablePath(Process process)
    {
        try
        {
            return process.MainModule?.FileName;
        }
        catch
        {
            return null;
        }
    }
}
