using System.Net;
using System.Net.Http.Json;
using SaverSearch.Application.Common.Models;
using SaverSearch.Application.Dtos;
using Xunit;

namespace SaverSearch.Tests.IntegrationTests;

public class CrudEndpointsTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public CrudEndpointsTests(CustomWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthCheck_ShouldReturnHealthy()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("Healthy", content);
    }

    [Fact]
    public async Task GetCategories_ShouldReturnPaginatedList()
    {
        // Act
        var response = await _client.GetFromJsonAsync<ApiResponse<PaginatedList<CategoryDto>>>("/api/v1/categories");

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.NotEmpty(response.Data.Items);
        // Assert seed category "Electronics" is returned
        Assert.Contains(response.Data.Items, c => c.Name == "Electronics");
    }

    [Fact]
    public async Task CreateCategory_ShouldSucceed_WithValidData()
    {
        // Arrange
        var newCategory = new CreateCategoryDto("Food & Drink", "Grocery items and restaurants");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/categories", newCategory);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<CategoryDto>>();
        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.Equal("Food & Drink", apiResponse.Data!.Name);
    }

    [Fact]
    public async Task CreateCategory_ShouldFail_WithInvalidData()
    {
        // Arrange
        var invalidCategory = new CreateCategoryDto("", "No name");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/categories", invalidCategory);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        Assert.NotNull(apiResponse);
        Assert.False(apiResponse.Success);
        Assert.Contains(apiResponse.Errors!, e => e.Contains("Category Name is required."));
    }
}
