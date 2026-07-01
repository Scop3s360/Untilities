using System.Diagnostics;
using AutoMapper;
using SaverSearch.Application.Common.Interfaces;
using SaverSearch.Application.Common.Mappings;
using SaverSearch.Application.Common.Models.Pipeline;
using SaverSearch.Application.Dtos;
using SaverSearch.Application.Services.Pipeline;
using SaverSearch.Domain.Entities;
using Xunit;
using Xunit.Abstractions;

namespace SaverSearch.Tests.UnitTests;

public class OfferResolverTests
{
    private readonly IMapper _mapper;
    private readonly ITestOutputHelper _output;

    public OfferResolverTests(ITestOutputHelper output)
    {
        _output = output;
        var config = new MapperConfiguration(
            cfg => cfg.AddProfile<MappingProfile>(),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance
        );
        _mapper = config.CreateMapper();
    }

    private (Retailer, Provider, OfferType) GetMockSetup()
    {
        var category = new Category { Id = Guid.NewGuid(), Name = "Supermarket" };
        var retailer = new Retailer
        {
            Id = Guid.NewGuid(),
            Name = "Tesco",
            Slug = "tesco",
            Website = "https://tesco.com",
            CategoryId = category.Id,
            Category = category
        };
        var provider = new Provider
        {
            Id = Guid.NewGuid(),
            Name = "TopCashback",
            Website = "https://topcashback.co.uk"
        };
        var offerType = new OfferType
        {
            Id = Guid.NewGuid(),
            Name = "Cashback"
        };
        return (retailer, provider, offerType);
    }

    [Fact]
    public async Task ResolveOffers_ShouldFilterOutExpiredAndFutureOffers()
    {
        // Arrange
        var (retailer, provider, offerType) = GetMockSetup();
        
        var activeOffer = new Offer
        {
            Id = Guid.NewGuid(),
            RetailerId = retailer.Id,
            Retailer = retailer,
            ProviderId = provider.Id,
            Provider = provider,
            OfferTypeId = offerType.Id,
            OfferType = offerType,
            Title = "Active Offer",
            Value = 5.0m,
            IsActive = true,
            StartDate = DateTime.UtcNow.AddDays(-2),
            EndDate = DateTime.UtcNow.AddDays(2),
            OfferUrl = "https://tesco.com"
        };

        var expiredOffer = new Offer
        {
            Id = Guid.NewGuid(),
            RetailerId = retailer.Id,
            Retailer = retailer,
            ProviderId = provider.Id,
            Provider = provider,
            OfferTypeId = offerType.Id,
            OfferType = offerType,
            Title = "Expired Offer",
            Value = 10.0m,
            IsActive = true,
            StartDate = DateTime.UtcNow.AddDays(-10),
            EndDate = DateTime.UtcNow.AddDays(-2),
            OfferUrl = "https://tesco.com"
        };

        var futureOffer = new Offer
        {
            Id = Guid.NewGuid(),
            RetailerId = retailer.Id,
            Retailer = retailer,
            ProviderId = provider.Id,
            Provider = provider,
            OfferTypeId = offerType.Id,
            OfferType = offerType,
            Title = "Future Offer",
            Value = 15.0m,
            IsActive = true,
            StartDate = DateTime.UtcNow.AddDays(2),
            EndDate = DateTime.UtcNow.AddDays(10),
            OfferUrl = "https://tesco.com"
        };

        var mockUow = new MockUnitOfWork(new List<Offer> { activeOffer, expiredOffer, futureOffer });
        var resolver = new OfferResolver(mockUow, _mapper);
        
        var context = new DiscoveryContext(null, "Tesco", null, 0.0m, null, null, null, new Dictionary<string, string>());
        var retailerDto = _mapper.Map<RetailerDto>(retailer);

        // Act
        var result = await resolver.ResolveOffersAsync(context, retailerDto);

        // Assert
        Assert.Single(result.Offers);
        Assert.Equal("Active Offer", result.Offers.First().Offer.Title);
        Assert.Equal(3, result.Diagnostics.OffersExamined);
        Assert.Equal(2, result.Diagnostics.OffersRejected);
        Assert.Contains(result.Diagnostics.RejectionReasons, r => r.Value.Contains("expired"));
        Assert.Contains(result.Diagnostics.RejectionReasons, r => r.Value.Contains("not yet active"));
    }

    [Fact]
    public async Task ResolveOffers_ShouldFilterOutInactiveRetailerOrProvider()
    {
        // Arrange
        var (retailer, provider, offerType) = GetMockSetup();
        retailer.IsActive = false; // Inactive Retailer

        var offer = new Offer
        {
            Id = Guid.NewGuid(),
            RetailerId = retailer.Id,
            Retailer = retailer,
            ProviderId = provider.Id,
            Provider = provider,
            OfferTypeId = offerType.Id,
            OfferType = offerType,
            Title = "Offer on Inactive Retailer",
            Value = 5.0m,
            IsActive = true,
            OfferUrl = "https://tesco.com"
        };

        var mockUow = new MockUnitOfWork(new List<Offer> { offer });
        var resolver = new OfferResolver(mockUow, _mapper);
        var context = new DiscoveryContext(null, "Tesco", null, 0.0m, null, null, null, new Dictionary<string, string>());
        var retailerDto = _mapper.Map<RetailerDto>(retailer);

        // Act
        var result = await resolver.ResolveOffersAsync(context, retailerDto);

        // Assert
        Assert.Empty(result.Offers);
        Assert.Equal(1, result.Diagnostics.OffersRejected);
    }

    [Fact]
    public async Task PerformanceScaleTest_ShouldScaleOptimally()
    {
        // Arrange
        var (retailer, provider, offerType) = GetMockSetup();
        
        // Seed 10,000 active offers for performance testing
        var list = Enumerable.Range(1, 10000).Select(i => new Offer
        {
            Id = Guid.NewGuid(),
            RetailerId = retailer.Id,
            Retailer = retailer,
            ProviderId = provider.Id,
            Provider = provider,
            OfferTypeId = offerType.Id,
            OfferType = offerType,
            Title = $"Offer #{i}",
            Value = 5.0m,
            IsActive = true,
            OfferUrl = "https://tesco.com"
        }).ToList();

        var mockUow = new MockUnitOfWork(list);
        var resolver = new OfferResolver(mockUow, _mapper);
        var context = new DiscoveryContext(null, "Tesco", null, 0.0m, null, null, null, new Dictionary<string, string>());
        var retailerDto = _mapper.Map<RetailerDto>(retailer);

        // Act & Measure
        var stopwatch = Stopwatch.StartNew();
        var result = await resolver.ResolveOffersAsync(context, retailerDto);
        stopwatch.Stop();

        _output.WriteLine($"Resolved {result.Offers.Count()} offers out of 10,000 in {stopwatch.ElapsedMilliseconds}ms.");

        // Assert
        Assert.Equal(10000, result.Offers.Count());
        Assert.True(stopwatch.ElapsedMilliseconds < 250, "Scale execution exceeds 250ms budget for 10,000 records.");
    }

    private class MockUnitOfWork(List<Offer> offers) : IUnitOfWork
    {
        public IGenericRepository<Category> Categories => throw new NotImplementedException();
        public IGenericRepository<Retailer> Retailers => throw new NotImplementedException();
        public IGenericRepository<Provider> Providers => throw new NotImplementedException();
        public IGenericRepository<OfferType> OfferTypes => throw new NotImplementedException();
        public IGenericRepository<Offer> Offers => new MockOfferRepo(offers);

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public void Dispose() { }
    }

    private class MockOfferRepo(List<Offer> offers) : IGenericRepository<Offer>
    {
        public Task<Offer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Offer?>(null);
        public Task<IEnumerable<Offer>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<Offer>>(offers);
        public IQueryable<Offer> GetQueryable(bool asNoTracking = true) => offers.AsQueryable();
        public Task AddAsync(Offer entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Update(Offer entity) { }
        public void Delete(Offer entity) { }
    }
}
