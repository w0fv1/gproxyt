using System.IO;

namespace Gproxyt;

internal static class ShortcutService
{
    public static string Create(string iconPath)
    {
        var executablePath = Environment.ProcessPath ?? throw new InvalidOperationException("无法确定 gproxyt 可执行文件位置。");
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        var shortcutPath = Path.Combine(desktop, "ChatGPT Proxy.lnk");
        var shellType = Type.GetTypeFromProgID("WScript.Shell") ?? throw new InvalidOperationException("Windows 快捷方式服务不可用。");
        dynamic shell = Activator.CreateInstance(shellType) ?? throw new InvalidOperationException("无法创建 Windows 快捷方式服务。");
        dynamic shortcut = shell.CreateShortcut(shortcutPath);
        shortcut.TargetPath = executablePath;
        shortcut.Arguments = "--launch";
        shortcut.WorkingDirectory = Path.GetDirectoryName(executablePath);
        shortcut.IconLocation = iconPath;
        shortcut.Save();
        return shortcutPath;
    }
}
