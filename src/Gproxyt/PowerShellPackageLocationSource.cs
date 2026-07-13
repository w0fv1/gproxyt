using System.Diagnostics;
using Gproxyt.Core;

namespace Gproxyt;

internal sealed class PowerShellPackageLocationSource : IPackageLocationSource
{
    public string? GetLatestInstallLocation()
    {
        const string command = "$package = Get-AppxPackage -Name 'OpenAI.Codex' | Sort-Object Version -Descending | Select-Object -First 1; if ($package) { [Console]::Out.Write($package.InstallLocation) }";
        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-NonInteractive");
        startInfo.ArgumentList.Add("-Command");
        startInfo.ArgumentList.Add(command);

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("无法启动 PowerShell 查询 ChatGPT 安装位置。");
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(TimeSpan.FromSeconds(10)))
        {
            process.Kill(true);
            throw new TimeoutException("查询 ChatGPT 安装位置超时。");
        }

        var output = outputTask.GetAwaiter().GetResult().Trim();
        var error = errorTask.GetAwaiter().GetResult().Trim();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"查询 ChatGPT 安装位置失败：{error}");
        }

        return string.IsNullOrWhiteSpace(output) ? null : output;
    }
}
