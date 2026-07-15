namespace Gproxyt.Windows.Tests;

public sealed class ApplicationLogTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), $"gproxyt-log-{Guid.NewGuid():N}");

    [Fact]
    public void Create_debug_log_writes_structured_events_and_exceptions_to_the_requested_directory()
    {
        Directory.CreateDirectory(root);
        string path;
        using (var log = ApplicationLog.Create(true, root))
        {
            path = Assert.IsType<string>(log.FilePath);
            log.Information("package_activation_started", ("PackageFullName", "OpenAI.Codex_1"));
            log.Error(new InvalidOperationException("activation failed"), "package_activation_failed");
        }

        Assert.Equal(root, Path.GetDirectoryName(path));
        Assert.Matches(@"^gproxyt-debug-\d{8}-\d{6}-\d+\.log$", Path.GetFileName(path));
        var content = File.ReadAllText(path);
        Assert.Contains("package_activation_started", content);
        Assert.Contains("OpenAI.Codex_1", content);
        Assert.Contains("package_activation_failed", content);
        Assert.Contains("activation failed", content);
    }

    [Fact]
    public void Create_without_debug_does_not_create_a_log_file()
    {
        Directory.CreateDirectory(root);
        using var log = ApplicationLog.Create(false, root);

        log.Information("ignored");

        Assert.Null(log.FilePath);
        Assert.Empty(Directory.GetFiles(root));
    }

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, true);
        }
    }
}
