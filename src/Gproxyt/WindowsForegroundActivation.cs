using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Gproxyt;

internal static class WindowsForegroundActivation
{
    private const int ShowWindowRestore = 9;
    private const uint AnyProcess = uint.MaxValue;

    public static bool GrantExistingInstanceForegroundPermission() => AllowSetForegroundWindow(AnyProcess);

    public static bool RestoreAndActivate(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);
        if (!window.IsVisible)
        {
            window.Show();
        }
        if (window.WindowState == WindowState.Minimized)
        {
            window.WindowState = WindowState.Normal;
        }

        var handle = new WindowInteropHelper(window).EnsureHandle();
        ShowWindowAsync(handle, ShowWindowRestore);
        var activated = SetForegroundWindow(handle);
        window.Activate();
        return activated || GetForegroundWindow() == handle;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AllowSetForegroundWindow(uint processId);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindowAsync(IntPtr window, int command);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr window);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();
}
