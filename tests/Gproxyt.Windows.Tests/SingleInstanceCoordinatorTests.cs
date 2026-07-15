namespace Gproxyt.Windows.Tests;

public sealed class SingleInstanceCoordinatorTests
{
    [Fact]
    public void Secondary_instance_signals_the_primary_instance()
    {
        var name = $"gproxyt-test-{Guid.NewGuid():N}";
        using var signal = new ManualResetEventSlim();
        using var primary = new SingleInstanceCoordinator(name);
        using var secondary = new SingleInstanceCoordinator(name);

        Assert.True(primary.IsPrimary);
        Assert.False(secondary.IsPrimary);

        primary.StartListening(signal.Set);
        secondary.NotifyPrimary();

        Assert.True(signal.Wait(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken));
    }

    [Fact]
    public void Disposing_the_primary_allows_a_new_primary_instance()
    {
        var name = $"gproxyt-test-{Guid.NewGuid():N}";
        using (var primary = new SingleInstanceCoordinator(name))
        {
            Assert.True(primary.IsPrimary);
        }

        using var replacement = new SingleInstanceCoordinator(name);

        Assert.True(replacement.IsPrimary);
    }
}
