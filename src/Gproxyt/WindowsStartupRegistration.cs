using Microsoft.Win32;

namespace Gproxyt;

internal sealed class WindowsStartupRegistration : IStartupRegistration
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "gproxyt";

    public Task ApplyAsync(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, true);
        if (enabled)
        {
            var executablePath = Environment.ProcessPath ?? throw new InvalidOperationException("无法确定 gproxyt 可执行文件位置。");
            key.SetValue(ValueName, $"\"{executablePath}\" --launch", RegistryValueKind.String);
        }
        else
        {
            key.DeleteValue(ValueName, false);
        }
        return Task.CompletedTask;
    }
}
