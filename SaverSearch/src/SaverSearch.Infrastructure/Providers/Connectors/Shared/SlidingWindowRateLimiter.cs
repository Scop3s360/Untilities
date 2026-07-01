namespace SaverSearch.Infrastructure.Providers.Connectors.Shared;

/// <summary>
/// A token-bucket sliding-window rate limiter safe for use in a single connector instance.
/// Limits outgoing HTTP calls to a configurable maximum per minute.
/// </summary>
public sealed class SlidingWindowRateLimiter : IDisposable
{
    private readonly int _maxCallsPerMinute;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly Queue<DateTime> _callTimestamps = new();
    private bool _disposed;

    public SlidingWindowRateLimiter(int maxCallsPerMinute)
    {
        if (maxCallsPerMinute <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxCallsPerMinute), "Must be greater than zero.");
        _maxCallsPerMinute = maxCallsPerMinute;
    }

    /// <summary>
    /// Waits until a call slot is available within the rate limit window.
    /// </summary>
    public async Task WaitForSlotAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            while (true)
            {
                var now = DateTime.UtcNow;
                var windowStart = now.AddMinutes(-1);

                // Purge timestamps outside the 1-minute window
                while (_callTimestamps.Count > 0 && _callTimestamps.Peek() < windowStart)
                    _callTimestamps.Dequeue();

                if (_callTimestamps.Count < _maxCallsPerMinute)
                {
                    _callTimestamps.Enqueue(now);
                    return;
                }

                // Calculate how long until the oldest call falls outside the window
                var oldestCall = _callTimestamps.Peek();
                var waitUntil = oldestCall.AddMinutes(1);
                var waitMs = (int)(waitUntil - now).TotalMilliseconds + 50; // +50ms buffer
                waitMs = Math.Max(waitMs, 100);

                _semaphore.Release();
                await Task.Delay(waitMs, cancellationToken);
                await _semaphore.WaitAsync(cancellationToken);
            }
        }
        catch
        {
            _semaphore.Release();
            throw;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _semaphore.Dispose();
    }
}
