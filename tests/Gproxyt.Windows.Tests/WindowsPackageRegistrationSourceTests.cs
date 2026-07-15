using System.Diagnostics;
using System.Text.Json;
using Gproxyt.Core;

namespace Gproxyt.Windows.Tests;

public sealed class WindowsPackageRegistrationSourceTests
{
    [Fact]
    public void Process_handle_supports_session_and_package_identity_queries()
    {
        using var process = Process.GetCurrentProcess();
        using var handle = WindowsPackageApi.OpenProcessForPackageQuery(process.Id);

        Assert.Equal(process.SessionId, WindowsPackageApi.GetProcessSessionId(handle));
        WindowsPackageApi.GetProcessPackageFamilyName(handle);
    }

    [Fact]
    public void FindCurrentUserRegistrations_matches_the_registered_main_package()
    {
        var source = new WindowsPackageRegistrationSource();

        for (var attempt = 0; attempt < 3; attempt++)
        {
            var before = ReadRegisteredPackages();
            var actual = source.FindCurrentUserRegistrations(ChatGptPackage.FamilyName);
            var after = ReadRegisteredPackages();
            var beforeKeys = ToComparableKeys(before);
            var afterKeys = ToComparableKeys(after);
            if (!beforeKeys.SequenceEqual(afterKeys, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }
            if (before.Count == 0)
            {
                Assert.Skip("当前用户未安装 Microsoft Store 版 ChatGPT。");
            }

            var actualKeys = ToComparableKeys(actual);
            Assert.True(beforeKeys.SequenceEqual(actualKeys, StringComparer.OrdinalIgnoreCase));
            Assert.Equal(actual.Count, actual.Select(item => item.PackageFullName).Distinct(StringComparer.OrdinalIgnoreCase).Count());
            Assert.All(actual, item => Assert.True(Directory.Exists(item.InstallLocation)));
            return;
        }

        throw new InvalidOperationException("Microsoft Store 程序包注册在验证期间持续变化。");
    }

    private static IReadOnlyList<PackageRegistration> ReadRegisteredPackages()
    {
        var command = $"$packages = @(Get-AppxPackage -PackageTypeFilter Main | Where-Object PackageFamilyName -eq '{ChatGptPackage.FamilyName}' | Select-Object PackageFullName, InstallLocation); [Console]::Out.Write((ConvertTo-Json -Compress -InputObject $packages))";
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

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("无法启动 Windows PowerShell。");
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(TimeSpan.FromSeconds(15)))
        {
            process.Kill(true);
            throw new TimeoutException("读取当前用户程序包注册超时。");
        }

        var output = outputTask.GetAwaiter().GetResult();
        var error = errorTask.GetAwaiter().GetResult().Trim();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Get-AppxPackage 失败：{error}");
        }

        var packages = JsonSerializer.Deserialize<AppxPackage[]>(output) ?? [];
        return packages
            .Select(item => new PackageRegistration(item.PackageFullName, item.InstallLocation))
            .ToArray();
    }

    private static string[] ToComparableKeys(IEnumerable<PackageRegistration> registrations) => registrations
        .Select(item => $"{item.PackageFullName}|{Path.TrimEndingDirectorySeparator(Path.GetFullPath(item.InstallLocation))}")
        .Order(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    private sealed record AppxPackage(string PackageFullName, string InstallLocation);
}
