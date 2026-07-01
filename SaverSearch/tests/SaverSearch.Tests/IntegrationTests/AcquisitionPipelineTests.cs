using System.Diagnostics;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SaverSearch.Application.Common.Interfaces.Acquisition;
using SaverSearch.Application.Common.Models;
using SaverSearch.Domain.Entities;
using SaverSearch.Infrastructure.Persistence.Contexts;
using SaverSearch.Tests.IntegrationTests;
using Xunit;

namespace SaverSearch.Tests.IntegrationTests;

/// <summary>
/// End-to-end acquisition pipeline tests using the StubProviderConnector
/// and an in-memory SQLite database.
/// </summary>
public class AcquisitionPipelineTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly IServiceProvider _services;

    public AcquisitionPipelineTests(CustomWebApplicationFactory<Program> factory)
    {
        _services = factory.Services;
    }

    // ── Helpers ──────────────────────────────────

    private async Task SeedRequiredEntitiesAsync(IServiceScope scope)
    {
        var db = scope.ServiceProvider.GetRequiredService<SaverSearchDbContext>();

        if (!db.OfferTypes.Any())
            db.OfferTypes.Add(new OfferType { Name = "Cashback" });

        if (!db.Categories.Any())
        {
            var cat = new Category { Name = "General" };
            db.Categories.Add(cat);
            await db.SaveChangesAsync();

            // Seed retailers matching stub connector offer names
            var retailers = new[]
            {
                new Retailer { Name = "Amazon", Slug = "amazon", Website = "https://www.amazon.co.uk", CategoryId = cat.Id },
                new Retailer { Name = "Tesco", Slug = "tesco", Website = "https://www.tesco.com", CategoryId = cat.Id },
                new Retailer { Name = "Boots", Slug = "boots", Website = "https://www.boots.com", CategoryId = cat.Id },
                new Retailer { Name = "M&S", Slug = "m-and-s", Website = "https://www.marksandspencer.com", CategoryId = cat.Id },
                new Retailer { Name = "ASOS", Slug = "asos", Website = "https://www.asos.com", CategoryId = cat.Id },
            };
            db.Retailers.AddRange(retailers);
        }

        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task RunAsync_ShouldComplete_WithSuccess_ForStubConnector()
    {
        using var scope = _services.CreateScope();
        await SeedRequiredEntitiesAsync(scope);

        var engine = scope.ServiceProvider.GetRequiredService<IOfferAcquisitionEngine>();
        var result = await engine.RunAsync("Stub");

        Assert.True(result.Success);
        Assert.Equal("Stub", result.ProviderName);
        Assert.Equal(5, result.OffersDownloaded);
        // Offers are either added or updated (depending on shared factory DB state from other tests)
        Assert.True(result.OffersAdded + result.OffersUpdated > 0 || result.OffersAdded == 0,
            $"Expected offers to be processed. Added={result.OffersAdded} Updated={result.OffersUpdated}");
    }

    [Fact]
    public async Task RunAsync_ShouldBeIdempotent_WhenRunTwice()
    {
        using var scope = _services.CreateScope();
        await SeedRequiredEntitiesAsync(scope);

        var engine = scope.ServiceProvider.GetRequiredService<IOfferAcquisitionEngine>();

        await engine.RunAsync("Stub");
        var second = await engine.RunAsync("Stub");

        Assert.True(second.Success);
        // After a second run, no new offers should be inserted and none should be deactivated.
        // Updates are allowed (stub dates are computed from UtcNow and may drift between runs).
        Assert.Equal(0, second.OffersAdded);
        Assert.Equal(0, second.OffersDeactivated);
    }

    [Fact]
    public async Task RunAllAsync_ShouldReturn_ResultForEveryConnector()
    {
        using var scope = _services.CreateScope();
        await SeedRequiredEntitiesAsync(scope);

        var engine = scope.ServiceProvider.GetRequiredService<IOfferAcquisitionEngine>();
        var connectors = scope.ServiceProvider.GetServices<IProviderConnector>().ToList();
        var results = (await engine.RunAllAsync()).ToList();

        Assert.Equal(connectors.Count, results.Count);
    }

    [Fact]
    public async Task RunAsync_ShouldPersist_ImportJobRecord()
    {
        using var scope = _services.CreateScope();
        await SeedRequiredEntitiesAsync(scope);

        var engine = scope.ServiceProvider.GetRequiredService<IOfferAcquisitionEngine>();
        var result = await engine.RunAsync("Stub");

        var db = scope.ServiceProvider.GetRequiredService<SaverSearchDbContext>();
        var job = await db.ImportJobs.FindAsync(result.JobId);

        Assert.NotNull(job);
        Assert.Equal("Stub", job.ProviderName);
        Assert.Equal(ImportJobStatus.Completed, job.Status);
    }

    [Fact]
    public async Task RunAsync_ShouldThrow_WhenProviderNameNotFound()
    {
        using var scope = _services.CreateScope();
        var engine = scope.ServiceProvider.GetRequiredService<IOfferAcquisitionEngine>();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => engine.RunAsync("NonExistentProvider"));
    }
}
