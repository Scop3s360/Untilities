using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SaverSearch.Application.Services.Acquisition;
using SaverSearch.Domain.Entities;
using SaverSearch.Infrastructure.Persistence.Contexts;
using Xunit;

namespace SaverSearch.Tests.UnitTests.Acquisition;

/// <summary>
/// Tests for OfferUpsertService using an in-memory SQLite database.
/// Covers insert, update, deactivate, and idempotency scenarios.
/// </summary>
public class OfferUpsertServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SaverSearchDbContext _db;
    private readonly OfferUpsertService _sut;

    private readonly Guid _providerId = Guid.NewGuid();
    private readonly Guid _retailerId = Guid.NewGuid();
    private readonly Guid _offerTypeId = Guid.NewGuid();

    public OfferUpsertServiceTests()
    {
        // Keep the connection open for the lifetime of this test class
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<SaverSearchDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new SaverSearchDbContext(options);
        _db.Database.EnsureCreated();

        _sut = new OfferUpsertService(_db, NullLogger<OfferUpsertService>.Instance);

        SeedDatabase();
    }

    private void SeedDatabase()
    {
        _db.Providers.Add(new Provider { Id = _providerId, Name = "TestProvider", Website = "https://test.com" });
        var cat = new Category { Name = "General" };
        _db.Categories.Add(cat);
        _db.SaveChanges();
        _db.Retailers.Add(new Retailer { Id = _retailerId, Name = "TestRetailer", Slug = "test", Website = "https://test.com", CategoryId = cat.Id });
        _db.OfferTypes.Add(new OfferType { Id = _offerTypeId, Name = "Cashback" });
        _db.SaveChanges();
    }

    private Offer MakeOffer(string externalId, decimal value = 5.0m) => new()
    {
        RetailerId = _retailerId,
        ProviderId = _providerId,
        OfferTypeId = _offerTypeId,
        ExternalId = externalId,
        Title = $"Offer {externalId}",
        OfferUrl = $"https://example.com/{externalId}",
        Value = value,
        ValueType = OfferValueType.Percentage,
        IsActive = true,
        LastUpdated = DateTime.UtcNow
    };

    [Fact]
    public async Task UpsertBatchAsync_ShouldInsert_NewOffers()
    {
        var incoming = new[] { MakeOffer("ext-001"), MakeOffer("ext-002") };
        var summary = await _sut.UpsertBatchAsync(incoming, _providerId);

        Assert.Equal(2, summary.Inserted);
        Assert.Equal(0, summary.Updated);
        Assert.Equal(0, summary.Deactivated);
        Assert.Equal(2, _db.Offers.Count(o => o.ProviderId == _providerId));
    }

    [Fact]
    public async Task UpsertBatchAsync_ShouldUpdate_ChangedOffer()
    {
        await _sut.UpsertBatchAsync(new[] { MakeOffer("ext-upd-001", value: 5.0m) }, _providerId);

        var updated = MakeOffer("ext-upd-001", value: 10.0m);
        var summary = await _sut.UpsertBatchAsync(new[] { updated }, _providerId);

        Assert.Equal(0, summary.Inserted);
        Assert.Equal(1, summary.Updated);
        var dbOffer = _db.Offers.Single(o => o.ExternalId == "ext-upd-001");
        Assert.Equal(10.0m, dbOffer.Value);
    }

    [Fact]
    public async Task UpsertBatchAsync_ShouldDeactivate_MissingOffer()
    {
        await _sut.UpsertBatchAsync(new[] { MakeOffer("ext-deact-001"), MakeOffer("ext-deact-002") }, _providerId);

        var summary = await _sut.UpsertBatchAsync(new[] { MakeOffer("ext-deact-001") }, _providerId);

        Assert.Equal(0, summary.Inserted);
        Assert.Equal(1, summary.Deactivated);
        Assert.False(_db.Offers.Single(o => o.ExternalId == "ext-deact-002").IsActive);
        Assert.True(_db.Offers.Single(o => o.ExternalId == "ext-deact-001").IsActive);
    }

    [Fact]
    public async Task UpsertBatchAsync_ShouldBeIdempotent_WhenSameDataReimported()
    {
        var offers = new[] { MakeOffer("ext-idem-001"), MakeOffer("ext-idem-002") };

        await _sut.UpsertBatchAsync(offers, _providerId);
        var summary2 = await _sut.UpsertBatchAsync(offers, _providerId);

        Assert.Equal(0, summary2.Inserted);
        Assert.Equal(0, summary2.Updated);
        Assert.Equal(0, summary2.Deactivated);
    }

    [Fact]
    public async Task UpsertBatchAsync_ShouldReactivate_PreviouslyDeactivatedOffer()
    {
        await _sut.UpsertBatchAsync(new[] { MakeOffer("ext-reac-001"), MakeOffer("ext-reac-002") }, _providerId);
        await _sut.UpsertBatchAsync(new[] { MakeOffer("ext-reac-001") }, _providerId);
        Assert.False(_db.Offers.Single(o => o.ExternalId == "ext-reac-002").IsActive);

        var summary = await _sut.UpsertBatchAsync(new[] { MakeOffer("ext-reac-001"), MakeOffer("ext-reac-002") }, _providerId);

        Assert.Equal(0, summary.Inserted);
        Assert.Equal(1, summary.Updated);
        Assert.True(_db.Offers.Single(o => o.ExternalId == "ext-reac-002").IsActive);
    }

    [Fact]
    public async Task UpsertBatchAsync_ShouldNotCreateDuplicates_OnConcurrentSameData()
    {
        var offers = new[] { MakeOffer("ext-dup-001") };

        await _sut.UpsertBatchAsync(offers, _providerId);
        await _sut.UpsertBatchAsync(offers, _providerId);
        await _sut.UpsertBatchAsync(offers, _providerId);

        Assert.Equal(1, _db.Offers.Count(o => o.ExternalId == "ext-dup-001"));
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
