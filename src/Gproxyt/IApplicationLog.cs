namespace Gproxyt;

internal interface IApplicationLog : IDisposable
{
    string? FilePath { get; }
    void Information(string eventName, params (string Name, object? Value)[] properties);
    void Error(Exception exception, string eventName, params (string Name, object? Value)[] properties);
}
