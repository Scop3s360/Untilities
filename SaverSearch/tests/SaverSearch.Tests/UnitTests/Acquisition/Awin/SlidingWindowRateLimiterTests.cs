using SaverSearch.Infrastructure.Providers.Connectors.Shared;
using Xunit;

namespace SaverSearch.Tests.UnitTests.Acquisition.Awin;

public class SlidingWindowRateLimiterTests
{
    [Fact]
    public async Task WaitForSlotAsync_ShouldAllow_ImmediateAccess_BelowLimit()
    {
        using var limiter = new SlidingWindowRateLimiter(10);

        // 5 calls should be immediate (below 10/min limit)
        for (int i = 0; i < 5; i++)
            await limiter.WaitForSlotAsync();

        // No exception = test passes
    }

    [Fact]
    public async Task WaitForSlotAsync_ShouldThrow_OperationCanceledException_WhenCancelled()
    {
        using var limiter = new SlidingWindowRateLimiter(1);
        var cts = new CancellationTokenSource();

        // Fill the single slot
        await limiter.WaitForSlotAsync();

        // Cancel immediately
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            limiter.WaitForSlotAsync(cts.Token));
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenLimitIsZero()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SlidingWindowRateLimiter(0));
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenLimitIsNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SlidingWindowRateLimiter(-1));
    }

    [Fact]
    public void Dispose_ShouldNotThrow_WhenCalledTwice()
    {
        var limiter = new SlidingWindowRateLimiter(10);
        limiter.Dispose();
        limiter.Dispose(); // Should not throw
    }
}
