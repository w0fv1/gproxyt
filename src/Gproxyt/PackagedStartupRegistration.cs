using Windows.ApplicationModel;

namespace Gproxyt;

internal sealed class PackagedStartupRegistration : IStartupRegistration
{
    private const string TaskId = "GproxytStartup";

    public async Task ApplyAsync(bool enabled)
    {
        var startupTask = await StartupTask.GetAsync(TaskId);
        if (enabled && startupTask.State == StartupTaskState.Disabled)
        {
            await startupTask.RequestEnableAsync();
            return;
        }
        if (!enabled && startupTask.State == StartupTaskState.Enabled)
        {
            startupTask.Disable();
        }
    }
}
