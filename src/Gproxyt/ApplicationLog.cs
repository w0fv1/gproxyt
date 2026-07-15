using System.IO;
using Serilog;

namespace Gproxyt;

internal static class ApplicationLog
{
    public static IApplicationLog None { get; } = new NullApplicationLog();

    public static IApplicationLog Create(bool debug, string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        if (!debug)
        {
            return None;
        }

        var fullDirectory = Path.GetFullPath(directory);
        Directory.CreateDirectory(fullDirectory);
        var path = Path.Combine(
            fullDirectory,
            $"gproxyt-debug-{DateTime.Now:yyyyMMdd-HHmmss}-{Environment.ProcessId}.log");
        var logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                path,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}",
                shared: true,
                flushToDiskInterval: TimeSpan.FromMilliseconds(250))
            .CreateLogger();
        return new SerilogApplicationLog(path, logger);
    }

    private sealed class NullApplicationLog : IApplicationLog
    {
        public string? FilePath => null;
        public void Information(string eventName, params (string Name, object? Value)[] properties)
        {
        }
        public void Error(Exception exception, string eventName, params (string Name, object? Value)[] properties)
        {
        }
        public void Dispose()
        {
        }
    }

    private sealed class SerilogApplicationLog(string filePath, ILogger logger) : IApplicationLog
    {
        public string? FilePath { get; } = filePath;

        public void Information(string eventName, params (string Name, object? Value)[] properties) =>
            AddProperties(eventName, properties).Information("{EventName}", eventName);

        public void Error(Exception exception, string eventName, params (string Name, object? Value)[] properties) =>
            AddProperties(eventName, properties).Error(exception, "{EventName}", eventName);

        public void Dispose() => (logger as IDisposable)?.Dispose();

        private ILogger AddProperties(string eventName, IEnumerable<(string Name, object? Value)> properties)
        {
            var contextual = logger.ForContext("EventName", eventName);
            foreach (var property in properties)
            {
                contextual = contextual.ForContext(property.Name, property.Value, true);
            }
            return contextual;
        }
    }
}
