using Gproxyt.Core;

namespace Gproxyt.Core.Tests;

public sealed class PackageProcessScopeTests
{
    [Theory]
    [InlineData("OpenAI.Codex_2p2nqsd0c76g0", true)]
    [InlineData("OpenAI.Codex_2p2nqsd0c76g1", false)]
    [InlineData("OpenAI.ChatGPT_2p2nqsd0c76g0", false)]
    [InlineData(null, false)]
    public void Contains_only_matches_the_package_family(string? packageFamilyName, bool expected)
    {
        var scope = new PackageProcessScope(ChatGptPackage.FamilyName);

        Assert.Equal(expected, scope.Contains(packageFamilyName));
    }
}
