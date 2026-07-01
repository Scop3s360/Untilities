using System.Diagnostics;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SaverSearch.Application.Common.Models.Acquisition;
using SaverSearch.Application.Services.Acquisition;
using SaverSearch.Domain.Entities;
using SaverSearch.Infrastructure.Persistence.Contexts;
using Xunit;

namespace SaverSearch.Tests.PerformanceTests;

/// <summary>
/// Performance benchmarks for the acquisition framework.
/// Validates that validation, normalisation, and upsert of 10,000 offers
/// complete within defined time bounds.
/// </summary>
public class AcquisitionPerformanceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SaverSearchDbContext _db;
    private readonly OfferUpsertService _upsertService;
    private readonly OfferValidationService _validationService;
    private readonly OfferNormalisationService _normalisationService;

    private readonly Guid _providerId = Guid.NewGuid();
    private readonly Guid _retailerId = Guid.NewGuid();
    private readonly Guid _offerTypeId = Guid.NewGuid();

    public AcquisitionPerformanceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<SaverSearchDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new SaverSearchDbContext(options);
        _db.Database.EnsureCreated();

        _upsertService = new OfferUpsertService(_db, NullLogger<OfferUpsertService>.Instance);
        _validationService = new OfferValidationService();
        _normalisationService = new OfferNormalisationService();

        SeedDatabase();
    }

    private void SeedDatabase()
    {
        _db.Providers.Add(new Provider { Id = _providerId, Name = "PerfProvider", Website = "https://perf.com" });
        var cat = new Category { Name = "General" };
        _db.Categories.Add(cat);
        _db.SaveChanges();
        _db.Retailers.Add(new Retailer { Id = _retailerId, Name = "PerfRetailer", Slug = "perf", Website = "https://perf.com", CategoryId = cat.Id });
        _db.OfferTypes.Add(new OfferType { Id = _offerTypeId, Name = "Cashback" });
        _db.SaveChanges();
    }

    private static IEnumerable<RawProviderOffer> GenerateRawOffers(int count) =>
        Enumerable.Range(1, count).Select(i => new RawProviderOffer(
            ExternalId: $"perf-{i:D7}",
            RetailerName: "PerfRetailer",
            RetailerUrl: "https://perf.com",
            RetailerDomain: "perf.com",
            Title: $"Perf Offer {i}",
            Description: null,
            Terms: null,
            OfferUrl: $"https://perf.com/offer/{i}",
            ValueType: "percentage",
            Value: (i % 20) + 1,
            MinimumSpend: null,
            MaximumReward: null,
            StartDate: null,
            EndDate: null,
            IsExclusive: false,
            RetrievedAt: DateTimeOffset.UtcNow,
            RawMetadata: new Dictionary<string, string>()
        ));

    [Fact]
    public void Validation_10000Offers_ShouldCompleteUnder2Seconds()
    {
        var offers = GenerateRawOffers(10_000).ToList();
        var sw = Stopwatch.StartNew();

        foreach (var offer in offers)
            _validationService.Validate(offer, "PerfProvider");

        sw.Stop();
        Assert.True(sw.ElapsedMilliseconds < 2_000,
            $"Validation of 10,000 offers took {sw.ElapsedMilliseconds}ms (expected < 2000ms).");
    }

    [Fact]
    public void Normalisation_10000Offers_ShouldCompleteUnder2Seconds()
    {
        var offers = GenerateRawOffers(10_000).ToList();
        var sw = Stopwatch.StartNew();

        foreach (var offer in offers)
            _normalisationService.Normalise(offer, _retailerId, _providerId, _offerTypeId);

        sw.Stop();
        Assert.True(sw.ElapsedMilliseconds < 2_000,
            $"Normalisation of 10,000 offers took {sw.ElapsedMilliseconds}ms (expected < 2000ms).");
    }

    [Fact]
    public async Task Upsert_10000Offers_ShouldCompleteUnder10Seconds()
    {
        var rawOffers = GenerateRawOffers(10_000).ToList();
        var domainOffers = rawOffers
            .Select(r => _normalisationService.Normalise(r, _retailerId, _providerId, _offerTypeId))
            .ToList();

        var sw = Stopwatch.StartNew();
        var summary = await _upsertService.UpsertBatchAsync(domainOffers, _providerId);
        sw.Stop();

        Assert.Equal(10_000, summary.Inserted);
        Assert.True(sw.ElapsedMilliseconds < 10_000,
            $"Upsert of 10,000 offers took {sw.ElapsedMilliseconds}ms (expected < 10000ms).");
    }

    [Fact]
    public async Task Upsert_ReimportSame10000Offers_ShouldBeIdempotent()
    {
        var rawOffers = GenerateRawOffers(10_000).ToList();
        var domainOffers = rawOffers
            .Select(r => _normalisationService.Normalise(r, _retailerId, _providerId, _offerTypeId))
            .ToList();

        await _upsertService.UpsertBatchAsync(domainOffers, _providerId);

        var domainOffers2 = rawOffers
            .Select(r => _normalisationService.Normalise(r, _retailerId, _providerId, _offerTypeId))
            .ToList();

        var sw = Stopwatch.StartNew();
        var summary = await _upsertService.UpsertBatchAsync(domainOffers2, _providerId);
        sw.Stop();

        Assert.Equal(0, summary.Inserted);
        Assert.Equal(0, summary.Updated);
        Assert.Equal(0, summary.Deactivated);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
