using System.Net;
using System.Net.Http.Json;
using SaverSearch.Application.Common.Models;
using SaverSearch.Application.Dtos.Discovery;
using Xunit;

namespace SaverSearch.Tests.IntegrationTests;

public class DiscoveryEndpointTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public DiscoveryEndpointTests(CustomWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    // ──────────────────────────────────────────────
    // Validation failures
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Discover_ShouldReturn400_WhenQueryIsEmpty()
    {
        // Arrange
        var request = new DiscoveryRequest { Query = "" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/discovery", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        Assert.NotNull(body);
        Assert.False(body.Success);
    }

    [Fact]
    public async Task Discover_ShouldReturn400_WhenQueryIsMissing()
    {
        // Arrange — send an object with no Query field
        var request = new { SpendAmount = 100 };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/discovery", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Discover_ShouldReturn400_WhenQueryIsTooShort()
    {
        // Arrange
        var request = new DiscoveryRequest { Query = "a" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/discovery", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ──────────────────────────────────────────────
    // Happy path — no data yet (empty DB)
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Discover_ShouldReturn200_WithValidQuery_WhenNoOffersExist()
    {
        // Arrange
        var request = new DiscoveryRequest
        {
            Query = "amazon",
            SpendAmount = 100.00m,
            UserGoal = UserGoal.Balanced
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/discovery", request);

        // Assert – pipeline should succeed (no offers found is still a success)
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<DiscoveryResponse>>();
        Assert.NotNull(body);
        Assert.True(body.Success);
        Assert.NotNull(body.Data);
    }

    [Fact]
    public async Task Discover_ShouldReturn200_WhenAllOptionalFieldsProvided()
    {
        // Arrange
        var request = new DiscoveryRequest
        {
            Query = "find me best cashback on groceries",
            SpendAmount = 250.00m,
            UserGoal = UserGoal.MaximumSavings,
            RetailerSlug = "tesco",
            UserRegion = "GB",
            PaymentMethod = "AmexGold",
            CorrelationId = "test-001"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/discovery", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<DiscoveryResponse>>();
        Assert.NotNull(body);
        Assert.True(body.Success);
        Assert.NotNull(body.Data);
        Assert.Equal("test-001", body.Data.CorrelationId);
    }

    [Fact]
    public async Task Discover_ShouldReturn200_WithDiagnosticsPopulated()
    {
        // Arrange
        var request = new DiscoveryRequest
        {
            Query = "amazon cashback",
            SpendAmount = 50.00m,
            UserGoal = UserGoal.HighestConfidence
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/discovery", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<DiscoveryResponse>>();
        Assert.NotNull(body);
        Assert.True(body.Success);
        Assert.NotNull(body.Data);
        // Diagnostics should always be present
        Assert.NotNull(body.Data.Diagnostics);
        Assert.True(body.Data.ExecutionTimeMs >= 0);
    }
}
