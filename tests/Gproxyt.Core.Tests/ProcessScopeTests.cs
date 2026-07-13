using Gproxyt.Core;

namespace Gproxyt.Core.Tests;

public sealed class ProcessScopeTests
{
    [Theory]
    [InlineData(@"C:\Program Files\WindowsApps\OpenAI.Codex_1\app\ChatGPT.exe", true)]
    [InlineData(@"C:\Program Files\WindowsApps\OpenAI.Codex_1\app\chrome.exe", true)]
    [InlineData(@"C:\Program Files\WindowsApps\OpenAI.Codex_10\app\ChatGPT.exe", false)]
    [InlineData(@"C:\Users\me\AppData\Local\OpenAI\Codex\codex.exe", false)]
    public void Contains_only_matches_processes_below_installation(string executablePath, bool expected)
    {
        var scope = new ProcessScope(@"C:\Program Files\WindowsApps\OpenAI.Codex_1");

        Assert.Equal(expected, scope.Contains(executablePath));
    }
}
