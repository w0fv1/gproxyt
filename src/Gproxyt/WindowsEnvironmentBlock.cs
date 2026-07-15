using System.Runtime.InteropServices;

namespace Gproxyt;

internal sealed class WindowsEnvironmentBlock : IDisposable
{
    public WindowsEnvironmentBlock(IReadOnlyDictionary<string, string> environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        var entries = environment
            .OrderBy(variable => variable.Key, StringComparer.OrdinalIgnoreCase)
            .Select(variable =>
            {
                if (string.IsNullOrWhiteSpace(variable.Key)
                    || variable.Key.Contains('=')
                    || variable.Key.Contains('\0')
                    || variable.Value.Contains('\0'))
                {
                    throw new ArgumentException("环境变量包含 Windows 不支持的字符。", nameof(environment));
                }
                return $"{variable.Key}={variable.Value}";
            });
        Pointer = Marshal.StringToHGlobalUni($"{string.Join('\0', entries)}\0");
    }

    public IntPtr Pointer { get; private set; }

    public void Dispose()
    {
        if (Pointer == IntPtr.Zero)
        {
            return;
        }
        Marshal.FreeHGlobal(Pointer);
        Pointer = IntPtr.Zero;
    }
}
