namespace Gproxyt.Core;

public sealed class PackageLaunchTargetUnavailableException(string message, Exception innerException)
    : Exception(message, innerException);
