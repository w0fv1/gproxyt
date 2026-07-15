namespace Gproxyt.Windows.Tests;

public sealed class ApplicationOptionsTests
{
    [Fact]
    public void Parse_recognizes_debug_and_launch_flags_case_insensitively()
    {
        var options = ApplicationOptions.Parse(["--DEBUG", "--Launch"]);

        Assert.True(options.Debug);
        Assert.True(options.Launch);
        Assert.False(options.CreateShortcut);
        Assert.False(options.RequiresInteractiveWindow);
    }

    [Fact]
    public void Parse_recognizes_shortcut_flag_without_enabling_debug_logging()
    {
        var options = ApplicationOptions.Parse(["--create-shortcut"]);

        Assert.False(options.Debug);
        Assert.False(options.Launch);
        Assert.True(options.CreateShortcut);
        Assert.False(options.RequiresInteractiveWindow);
    }

    [Fact]
    public void Parse_uses_the_interactive_window_when_no_command_mode_is_requested()
    {
        var options = ApplicationOptions.Parse(["--debug"]);

        Assert.True(options.RequiresInteractiveWindow);
    }
}
