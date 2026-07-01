using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaverSearch.Infrastructure.Providers.Connectors.Shared;

namespace SaverSearch.Infrastructure.Providers.Connectors.Awin;

/// <summary>
/// Handles all direct HTTP communication with the AWIN Publisher REST API.
/// Applies Bearer token authentication, rate limiting, and retry logic.
/// This class is intentionally separate from the connector to keep HTTP concerns isolated.
/// </summary>
public sealed class AwinApiClient : IDisposable
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AwinConnectorOptions _options;
    private readonly SlidingWindowRateLimiter _rateLimiter;
    private readonly RetryOptions _retryOptions;
    private readonly ILogger<AwinApiClient> _logger;
    private bool _disposed;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AwinApiClient(
        IHttpClientFactory httpClientFactory,
        IOptions<AwinConnectorOptions> options,
        ILogger<AwinApiClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;

        _rateLimiter = new SlidingWindowRateLimiter(_options.RateLimitPerMinute);
        _retryOptions = new RetryOptions
        {
            MaxRetries = _options.MaxRetries,
            BaseDelayMs = _options.RetryBaseDelayMs
        };
    }

    /// <summary>Retrieves all joined merchant programmes for the configured publisher.</summary>
    public async Task<IReadOnlyList<AwinProgramme>> GetJoinedProgrammesAsync(CancellationToken ct = default)
    {
        var url = $"{_options.BaseUrl}/publishers/{_options.PublisherId}/programmes" +
                  $"?relationship=joined&countryCode={_options.RegionCode}";

        _logger.LogDebug("AWIN: Fetching joined programmes from {Url}", url);

        var result = await ExecuteWithRetryAsync<List<AwinProgramme>>(url, ct);
        return result ?? [];
    }

    /// <summary>
    /// Retrieves all active promotions for joined programmes in the configured region.
    /// </summary>
    public async Task<IReadOnlyList<AwinPromotion>> GetActivePromotionsAsync(CancellationToken ct = default)
    {
        var url = $"{_options.BaseUrl}/publishers/{_options.PublisherId}/promotions" +
                  $"?membershipStatus=joined&regionCode={_options.RegionCode}";

        _logger.LogDebug("AWIN: Fetching active promotions from {Url}", url);

        var result = await ExecuteWithRetryAsync<List<AwinPromotion>>(url, ct);
        return result ?? [];
    }

    /// <summary>Performs a lightweight health check against the programmes endpoint.</summary>
    public async Task<(bool IsHealthy, long LatencyMs, string? ErrorMessage)> PingAsync(CancellationToken ct = default)
    {
        var url = $"{_options.BaseUrl}/publishers/{_options.PublisherId}/programmes" +
                  $"?relationship=joined&countryCode={_options.RegionCode}&pageSize=1";

        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            await _rateLimiter.WaitForSlotAsync(ct);
            var httpClient = _httpClientFactory.CreateClient("AWIN");
            var response = await httpClient.GetAsync(url, ct);
            sw.Stop();

            if (response.IsSuccessStatusCode)
                return (true, sw.ElapsedMilliseconds, null);

            var error = await response.Content.ReadAsStringAsync(ct);
            return (false, sw.ElapsedMilliseconds, $"HTTP {(int)response.StatusCode}: {error[..Math.Min(200, error.Length)]}");
        }
        catch (Exception ex)
        {
            sw.Stop();
            return (false, sw.ElapsedMilliseconds, ex.Message);
        }
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private async Task<T?> ExecuteWithRetryAsync<T>(string url, CancellationToken ct)
    {
        var retryResult = await ExponentialBackoffRetryPolicy.ExecuteAsync<T?>(
            async token =>
            {
                await _rateLimiter.WaitForSlotAsync(token);

                var httpClient = _httpClientFactory.CreateClient("AWIN");
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.AccessToken);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await httpClient.SendAsync(request, token);
                return await HandleResponseAsync<T>(response, url, token);
            },
            _retryOptions,
            cancellationToken: ct);

        if (!retryResult.Success)
        {
            _logger.LogError("AWIN request failed after {Attempts} attempts for {Url}: {Error}",
                retryResult.AttemptsUsed, url, retryResult.LastException?.Message);

            if (retryResult.LastException != null)
                throw retryResult.LastException;
        }

        return retryResult.Value;
    }

    private async Task<T?> HandleResponseAsync<T>(HttpResponseMessage response, string url, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync(ct);
            _logger.LogDebug("AWIN: Received {Bytes} bytes from {Url}", json.Length, url);
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }

        var body = await response.Content.ReadAsStringAsync(ct);

        switch (response.StatusCode)
        {
            case HttpStatusCode.Unauthorized:
                throw new AwinAuthenticationException();

            case HttpStatusCode.NotFound:
                throw new AwinNotFoundException(url);

            case HttpStatusCode.TooManyRequests:
                var retryAfter = 60;
                if (response.Headers.RetryAfter?.Delta.HasValue == true)
                    retryAfter = (int)response.Headers.RetryAfter.Delta.Value.TotalSeconds;
                throw new AwinRateLimitException(retryAfter);

            default:
                var statusCode = (int)response.StatusCode;
                if (statusCode >= 500)
                    throw new AwinServiceException(statusCode, body[..Math.Min(500, body.Length)]);

                throw new AwinConnectorException(
                    $"Unexpected AWIN response {statusCode} for {url}: {body[..Math.Min(200, body.Length)]}",
                    statusCode);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _rateLimiter.Dispose();
    }
}
