using Microsoft.Extensions.DependencyInjection;
using SaverSearch.Application.Common.Interfaces.Acquisition;
using SaverSearch.Infrastructure.Providers.Connectors;
using Xunit;

namespace SaverSearch.Tests.UnitTests.Acquisition;

/// <summary>
/// Verifies that the DI container correctly auto-discovers and registers
/// all IProviderConnector implementations from the Infrastructure assembly.
/// </summary>
public class ConnectorDiscoveryTests : IClassFixture<SaverSearch.Tests.IntegrationTests.CustomWebApplicationFactory<Program>>
{
    private readonly IServiceProvider _services;

    public ConnectorDiscoveryTests(SaverSearch.Tests.IntegrationTests.CustomWebApplicationFactory<Program> factory)
    {
        _services = factory.Services;
    }

    [Fact]
    public void DI_ShouldRegister_AtLeastOneConnector()
    {
        using var scope = _services.CreateScope();
        var connectors = scope.ServiceProvider.GetServices<IProviderConnector>().ToList();
        Assert.NotEmpty(connectors);
    }

    [Fact]
    public void DI_ShouldRegister_StubConnector()
    {
        using var scope = _services.CreateScope();
        var connectors = scope.ServiceProvider.GetServices<IProviderConnector>().ToList();
        Assert.Contains(connectors, c => c is StubProviderConnector);
    }

    [Fact]
    public void DI_ShouldRegister_ConnectorsWithUniqueProviderNames()
    {
        using var scope = _services.CreateScope();
        var connectors = scope.ServiceProvider.GetServices<IProviderConnector>().ToList();
        var names = connectors.Select(c => c.ProviderName).ToList();
        var distinct = names.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        Assert.Equal(distinct.Count, names.Count);
    }

    [Fact]
    public async Task StubConnector_HealthCheck_ShouldReturnHealthy()
    {
        using var scope = _services.CreateScope();
        var connectors = scope.ServiceProvider.GetServices<IProviderConnector>().ToList();
        var stub = connectors.OfType<StubProviderConnector>().First();

        var health = await stub.HealthCheckAsync();

        Assert.True(health.IsHealthy);
        Assert.Equal("Stub", health.ProviderName);
    }

    [Fact]
    public async Task StubConnector_GetOffersAsync_ShouldReturnExpectedOffers()
    {
        using var scope = _services.CreateScope();
        var connectors = scope.ServiceProvider.GetServices<IProviderConnector>().ToList();
        var stub = connectors.OfType<StubProviderConnector>().First();

        var offers = (await stub.GetOffersAsync()).ToList();

        Assert.Equal(5, offers.Count);
        Assert.All(offers, o => Assert.False(string.IsNullOrWhiteSpace(o.ExternalId)));
        Assert.All(offers, o => Assert.True(o.Value > 0));
    }
}
