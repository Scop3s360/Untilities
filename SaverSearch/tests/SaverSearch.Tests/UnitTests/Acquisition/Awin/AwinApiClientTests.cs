using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SaverSearch.Infrastructure.Providers.Connectors.Awin;
using Xunit;

namespace SaverSearch.Tests.UnitTests.Acquisition.Awin;

/// <summary>
/// Tests for AwinApiClient error classification and response handling.
/// Uses a custom IHttpClientFactory that wraps a mock handler.
/// </summary>
public class AwinApiClientTests : IDisposable
{
    private readonly MockHttpHandler _mockHandler = new();
    private readonly IOptions<AwinConnectorOptions> _options = Options.Create(new AwinConnectorOptions
    {
        PublisherId = 12345,
        AccessToken = "test-bearer-token",
        BaseUrl = "https://api.awin.com",
        RegionCode = "GB",
        RateLimitPerMinute = 18,
        MaxRetries = 0, // no retries in unit tests
        RetryBaseDelayMs = 10,
        TimeoutSeconds = 5
    });

    private AwinApiClient CreateSut()
    {
        var factory = new MockHttpClientFactory(_mockHandler);
        return new AwinApiClient(factory, _options, NullLogger<AwinApiClient>.Instance);
    }

    // ── 200 OK ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetJoinedProgrammesAsync_ShouldReturn_ParsedList_On200()
    {
        var json = JsonSerializer.Serialize(new[]
        {
            new { id = 1, name = "Amazon", displayUrl = "https://www.amazon.co.uk", relationship = "joined" },
            new { id = 2, name = "Tesco", displayUrl = "https://www.tesco.com", relationship = "joined" }
        });
        _mockHandler.SetResponse(HttpStatusCode.OK, json);

        var sut = CreateSut();
        var result = await sut.GetJoinedProgrammesAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("Amazon", result[0].Name);
        Assert.Equal("Tesco", result[1].Name);
    }

    [Fact]
    public async Task GetActivePromotionsAsync_ShouldReturn_EmptyList_OnEmptyArray()
    {
        _mockHandler.SetResponse(HttpStatusCode.OK, "[]");

        var sut = CreateSut();
        var result = await sut.GetActivePromotionsAsync();

        Assert.Empty(result);
    }

    // ── 401 Unauthorized ────────────────────────────────────────────────────

    [Fact]
    public async Task GetJoinedProgrammesAsync_ShouldThrow_AwinAuthenticationException_On401()
    {
        _mockHandler.SetResponse(HttpStatusCode.Unauthorized, "Unauthorized");

        var sut = CreateSut();
        await Assert.ThrowsAsync<AwinAuthenticationException>(() =>
            sut.GetJoinedProgrammesAsync());
    }

    // ── 404 Not Found ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetJoinedProgrammesAsync_ShouldThrow_AwinNotFoundException_On404()
    {
        _mockHandler.SetResponse(HttpStatusCode.NotFound, "Not Found");

        var sut = CreateSut();
        await Assert.ThrowsAsync<AwinNotFoundException>(() =>
            sut.GetJoinedProgrammesAsync());
    }

    // ── 429 Too Many Requests ─────────────────────────────────────────────────

    [Fact]
    public async Task GetJoinedProgrammesAsync_ShouldThrow_AwinRateLimitException_On429()
    {
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(30));
        response.Content = new StringContent("Rate limited");
        _mockHandler.SetRawResponse(response);

        var sut = CreateSut();
        var ex = await Assert.ThrowsAsync<AwinRateLimitException>(() =>
            sut.GetJoinedProgrammesAsync());

        Assert.Equal(30, ex.RetryAfterSeconds);
    }

    // ── 500 Server Error ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetJoinedProgrammesAsync_ShouldThrow_AwinServiceException_On500()
    {
        _mockHandler.SetResponse(HttpStatusCode.InternalServerError, "Internal Server Error");

        var sut = CreateSut();
        await Assert.ThrowsAsync<AwinServiceException>(() =>
            sut.GetJoinedProgrammesAsync());
    }

    // ── Health Check ──────────────────────────────────────────────────────────

    [Fact]
    public async Task PingAsync_ShouldReturn_Healthy_On200()
    {
        _mockHandler.SetResponse(HttpStatusCode.OK, "[]");

        var sut = CreateSut();
        var (isHealthy, latencyMs, errorMessage) = await sut.PingAsync();

        Assert.True(isHealthy);
        Assert.Null(errorMessage);
        Assert.True(latencyMs >= 0);
    }

    [Fact]
    public async Task PingAsync_ShouldReturn_Unhealthy_On401()
    {
        _mockHandler.SetResponse(HttpStatusCode.Unauthorized, "Unauthorized");

        var sut = CreateSut();
        var (isHealthy, _, error) = await sut.PingAsync();

        Assert.False(isHealthy);
        Assert.NotNull(error);
    }

    public void Dispose() { }
}

// ── Test Doubles ─────────────────────────────────────────────────────────────

/// <summary>
/// Mock IHttpClientFactory that always returns an HttpClient wrapping the given handler.
/// </summary>
internal sealed class MockHttpClientFactory : IHttpClientFactory
{
    private readonly MockHttpHandler _handler;

    public MockHttpClientFactory(MockHttpHandler handler) => _handler = handler;

    public HttpClient CreateClient(string name)
    {
        return new HttpClient(_handler, disposeHandler: false)
        {
            BaseAddress = new Uri("https://api.awin.com")
        };
    }
}

/// <summary>
/// Simple mock HTTP handler that returns preconfigured responses.
/// </summary>
internal sealed class MockHttpHandler : DelegatingHandler
{
    private HttpStatusCode _statusCode = HttpStatusCode.OK;
    private string _body = "[]";
    private HttpResponseMessage? _rawResponse;

    public void SetResponse(HttpStatusCode statusCode, string body)
    {
        _statusCode = statusCode;
        _body = body;
        _rawResponse = null;
    }

    public void SetRawResponse(HttpResponseMessage response)
    {
        _rawResponse = response;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (_rawResponse != null)
        {
            // Re-read the content synchronously before returning
            var rawContent = _rawResponse.Content?.ReadAsStringAsync().GetAwaiter().GetResult() ?? "";
            var copy = new HttpResponseMessage(_rawResponse.StatusCode)
            {
                Content = new StringContent(rawContent, Encoding.UTF8, "application/json")
            };
            foreach (var h in _rawResponse.Headers)
                copy.Headers.TryAddWithoutValidation(h.Key, h.Value);
            return Task.FromResult(copy);
        }

        return Task.FromResult(new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_body, Encoding.UTF8, "application/json")
        });
    }
}
