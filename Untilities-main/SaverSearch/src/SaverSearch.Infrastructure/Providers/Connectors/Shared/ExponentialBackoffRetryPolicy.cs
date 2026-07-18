namespace SaverSearch.Infrastructure.Providers.Connectors.Shared;

/// <summary>
/// Marker interface — exceptions that implement this will NOT be retried.
/// Implement this on authentication, not-found, or other terminal connector exceptions.
/// </summary>
public interface ITerminalConnectorException { }

/// <summary>
/// Marker interface — exceptions that implement this signal a rate limit.
/// Implementations should provide RetryAfterSeconds.
/// </summary>
public interface IRateLimitException
{
    int RetryAfterSeconds { get; }
}

/// <summary>
/// Configures exponential backoff retry behaviour for affiliate network HTTP clients.
/// </summary>
public sealed class RetryOptions
{
    public int MaxRetries { get; init; } = 3;
    public int BaseDelayMs { get; init; } = 2000;
    public int MaxJitterMs { get; init; } = 500;
}

/// <summary>
/// Encapsulates the result of a retry-aware HTTP operation.
/// </summary>
public sealed class RetryResult<T>
{
    public T? Value { get; init; }
    public bool Success { get; init; }
    public int AttemptsUsed { get; init; }
    public Exception? LastException { get; init; }
}

/// <summary>
/// Provides exponential backoff retry logic with jitter for affiliate network HTTP calls.
/// Uses marker interfaces (<see cref="ITerminalConnectorException"/>, <see cref="IRateLimitException"/>)
/// to classify exceptions — fully provider-agnostic.
/// </summary>
public static class ExponentialBackoffRetryPolicy
{
    private static readonly Random Jitter = new();

    /// <summary>
    /// Executes the given operation with retry logic.
    /// Terminal exceptions (ITerminalConnectorException) are never retried.
    /// Rate limit exceptions (IRateLimitException) respect the Retry-After duration.
    /// All other transient exceptions use exponential backoff.
    /// </summary>
    public static async Task<RetryResult<T>> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        RetryOptions options,
        CancellationToken cancellationToken = default)
    {
        int attempt = 0;
        Exception? lastException = null;

        while (attempt <= options.MaxRetries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var result = await operation(cancellationToken);
                return new RetryResult<T> { Value = result, Success = true, AttemptsUsed = attempt + 1 };
            }
            catch (Exception ex) when (ex is ITerminalConnectorException)
            {
                throw; // Terminal failures are never retried
            }
            catch (Exception ex) when (ex is IRateLimitException rateLimitEx)
            {
                lastException = ex;
                if (attempt >= options.MaxRetries) break;

                var delay = rateLimitEx.RetryAfterSeconds > 0
                    ? rateLimitEx.RetryAfterSeconds * 1000
                    : 60_000;

                await Task.Delay(delay, cancellationToken);
            }
            catch (HttpRequestException httpEx)
            {
                lastException = httpEx;
                if (attempt >= options.MaxRetries) break;
                await DelayWithJitterAsync(attempt, options, cancellationToken);
            }
            catch (TaskCanceledException tcEx) when (!cancellationToken.IsCancellationRequested)
            {
                // Timeout — treat as transient
                lastException = tcEx;
                if (attempt >= options.MaxRetries) break;
                await DelayWithJitterAsync(attempt, options, cancellationToken);
            }

            attempt++;
        }

        return new RetryResult<T> { Success = false, AttemptsUsed = attempt, LastException = lastException };
    }

    private static async Task DelayWithJitterAsync(int attempt, RetryOptions options, CancellationToken ct)
    {
        var exponential = options.BaseDelayMs * (int)Math.Pow(2, attempt);
        var jitter = Jitter.Next(0, options.MaxJitterMs);
        await Task.Delay(exponential + jitter, ct);
    }
}
