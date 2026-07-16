namespace Gproxyt;

internal interface IStartupRegistration
{
    Task ApplyAsync(bool enabled);
}
