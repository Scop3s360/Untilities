using System.Diagnostics;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SaverSearch.Domain.Entities;
using SaverSearch.Infrastructure.Persistence.Contexts;
using Xunit;
using Xunit.Abstractions;

namespace SaverSearch.Tests.PerformanceTests;

public class DatabasePerformanceTests
{
    private readonly ITestOutputHelper _output;

    public DatabasePerformanceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task Database_ShouldPerformOptimally_UnderScale()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<SaverSearchDbContext>()
            .UseSqlite(connection)
            .Options;

        using (var context = new SaverSearchDbContext(options))
        {
            context.Database.EnsureCreated();

            // 1. Seed 100 Providers
            var providers = Enumerable.Range(1, 100).Select(i => new Provider
            {
                Id = Guid.NewGuid(),
                Name = $"Provider {i}",
                Website = $"https://provider{i}.com"
            }).ToList();

            // 2. Seed 1,000 Retailers (using Electronics category ID from seed data)
            var categoryId = Guid.Parse("23a2a3ad-cc52-472e-8390-50d41f3d3281");
            var retailers = Enumerable.Range(1, 1000).Select(i => new Retailer
            {
                Id = Guid.NewGuid(),
                Name = $"Retailer {i}",
                Slug = $"retailer-{i}",
                Website = $"https://retailer{i}.com",
                CategoryId = categoryId
            }).ToList();

            // 3. Seed 10,000 Offers
            var offerTypeId = Guid.Parse("11c223ad-cc52-472e-8390-50d41f3d3281"); // Cashback
            var random = new Random(42);
            var offers = Enumerable.Range(1, 10000).Select(i => new Offer
            {
                Id = Guid.NewGuid(),
                RetailerId = retailers[random.Next(retailers.Count)].Id,
                ProviderId = providers[random.Next(providers.Count)].Id,
                OfferTypeId = offerTypeId,
                Title = $"Offer {i}",
                Value = 5.0m,
                ValueType = OfferValueType.Percentage,
                OfferUrl = "https://deals.com"
            }).ToList();

            _output.WriteLine("Starting bulk insertions...");
            var stopwatch = Stopwatch.StartNew();

            await context.Providers.AddRangeAsync(providers);
            await context.Retailers.AddRangeAsync(retailers);
            await context.Offers.AddRangeAsync(offers);
            await context.SaveChangesAsync();

            stopwatch.Stop();
            _output.WriteLine($"Seeded database in {stopwatch.ElapsedMilliseconds}ms.");
            Assert.True(stopwatch.ElapsedMilliseconds < 5000, "Seeding took too long.");

            // 4. Query Performance (Read queries using AsNoTracking)
            _output.WriteLine("Executing read query selections...");
            var queryStopwatch = Stopwatch.StartNew();

            var queryResult = await context.Offers
                .AsNoTracking()
                .Include(o => o.Retailer)
                .Where(o => o.ValueType == OfferValueType.Percentage)
                .OrderBy(o => o.Title)
                .Take(50)
                .ToListAsync();

            queryStopwatch.Stop();
            _output.WriteLine($"Queried 50 offers with includes in {queryStopwatch.ElapsedMilliseconds}ms.");

            // Assertions
            Assert.Equal(50, queryResult.Count);
            Assert.True(queryStopwatch.ElapsedMilliseconds < 150, "Read query execution time exceeds performance budget of 150ms.");
        }
    }
}
