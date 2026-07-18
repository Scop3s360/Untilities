using Microsoft.Extensions.Options;
using SaverSearch.Application.Common.Interfaces;
using SaverSearch.Application.Common.Models.Pipeline;
using SaverSearch.Application.Common.Models.Resolver;
using SaverSearch.Application.Services.Resolver;
using SaverSearch.Application.Services.Resolver.Strategies;
using SaverSearch.Domain.Entities;
using Xunit;

namespace SaverSearch.Tests.UnitTests;

public class RetailerResolverTests
{
    private readonly IOptions<ConfidenceSettings> _options = Options.Create(new ConfidenceSettings());
    private readonly List<IRetailerResolverStrategy> _strategies;

    public RetailerResolverTests()
    {
        _strategies = new List<IRetailerResolverStrategy>
        {
            new ExactNameMatchStrategy(_options),
            new SlugMatchStrategy(_options),
            new WebsiteMatchStrategy(_options),
            new AliasMatchStrategy(_options),
            new NormalizedTextMatchStrategy(_options),
            new FuzzyMatchStrategy(_options)
        };
    }

    private List<Retailer> GetMockRetailers()
    {
        var categoryId = Guid.NewGuid();
        var amazon = new Retailer
        {
            Id = Guid.Parse("a0f2b3ad-cc52-472e-8390-50d41f3d3281"),
            Name = "Amazon",
            Slug = "amazon",
            Website = "https://amazon.co.uk",
            CategoryId = categoryId
        };
        amazon.Aliases.Add(new RetailerAlias { RetailerId = amazon.Id, AliasName = "Amazon UK" });
        amazon.Aliases.Add(new RetailerAlias { RetailerId = amazon.Id, AliasName = "amazon.co.uk" });

        var currys = new Retailer
        {
            Id = Guid.Parse("c0f2b3ad-cc52-472e-8390-50d41f3d3281"),
            Name = "Currys",
            Slug = "currys",
            Website = "https://currys.co.uk",
            CategoryId = categoryId
        };
        currys.Aliases.Add(new RetailerAlias { RetailerId = currys.Id, AliasName = "Currys PC World" });
        currys.Aliases.Add(new RetailerAlias { RetailerId = currys.Id, AliasName = "PC World" });

        return new List<Retailer> { amazon, currys };
    }

    [Fact]
    public async Task ExactNameMatch_ShouldReturnHighestConfidence()
    {
        // Arrange
        var mockUow = new MockUnitOfWork(GetMockRetailers());
        var resolver = new RetailerResolver(mockUow, _strategies);
        var context = new DiscoveryContext(null, "Amazon", null, 0.0m, null, null, null, new Dictionary<string, string>());

        // Act
        var result = await resolver.ResolveAsync(context);

        // Assert
        Assert.NotNull(result.MatchedRetailer);
        Assert.Equal("Amazon", result.MatchedRetailer.Name);
        Assert.Equal(100.0, result.ConfidenceScore);
        Assert.Equal("Exact Name Match", result.MatchType);
    }

    [Fact]
    public async Task AliasMatch_ShouldResolvePCWorld_ToCurrys()
    {
        // Arrange
        var mockUow = new MockUnitOfWork(GetMockRetailers());
        var resolver = new RetailerResolver(mockUow, _strategies);
        var context = new DiscoveryContext(null, "PC World", null, 0.0m, null, null, null, new Dictionary<string, string>());

        // Act
        var result = await resolver.ResolveAsync(context);

        // Assert
        Assert.NotNull(result.MatchedRetailer);
        Assert.Equal("Currys", result.MatchedRetailer.Name);
        Assert.Equal(95.0, result.ConfidenceScore);
        Assert.Equal("Alias Match", result.MatchType);
    }

    [Fact]
    public async Task WebsiteMatch_ShouldResolveDomainQuery()
    {
        // Arrange
        var mockUow = new MockUnitOfWork(GetMockRetailers());
        var resolver = new RetailerResolver(mockUow, _strategies);
        var context = new DiscoveryContext(null, "https://currys.co.uk", null, 0.0m, null, null, null, new Dictionary<string, string>());

        // Act
        var result = await resolver.ResolveAsync(context);

        // Assert
        Assert.NotNull(result.MatchedRetailer);
        Assert.Equal("Currys", result.MatchedRetailer.Name);
        Assert.Equal(100.0, result.ConfidenceScore);
    }

    [Fact]
    public async Task FuzzyMatch_ShouldResolveTypo_ToAmazon()
    {
        // Arrange
        var mockUow = new MockUnitOfWork(GetMockRetailers());
        var resolver = new RetailerResolver(mockUow, _strategies);
        // "Amazan" is close to "Amazon" (1 char difference: Levenshtein distance = 1, len = 6, similarity = 5/6 = 83.33%)
        var context = new DiscoveryContext(null, "Amazan", null, 0.0m, null, null, null, new Dictionary<string, string>());

        // Act
        var result = await resolver.ResolveAsync(context);

        // Assert
        Assert.NotNull(result.MatchedRetailer);
        Assert.Equal("Amazon", result.MatchedRetailer.Name);
        Assert.True(result.ConfidenceScore > 80.0);
        Assert.Equal("Fuzzy Match", result.MatchType);
    }

    [Fact]
    public async Task UnknownRetailer_ShouldReturnNullWinner()
    {
        // Arrange
        var mockUow = new MockUnitOfWork(GetMockRetailers());
        var resolver = new RetailerResolver(mockUow, _strategies);
        var context = new DiscoveryContext(null, "Nonexistent Retailer LLC", null, 0.0m, null, null, null, new Dictionary<string, string>());

        // Act
        var result = await resolver.ResolveAsync(context);

        // Assert
        Assert.Null(result.MatchedRetailer);
        Assert.Equal(0.0, result.ConfidenceScore);
        Assert.Equal("None", result.MatchType);
    }

    // Mock UnitOfWork to supply database values during isolated tests
    private class MockUnitOfWork(List<Retailer> retailers) : IUnitOfWork
    {
        public IGenericRepository<Category> Categories => throw new NotImplementedException();
        public IGenericRepository<Retailer> Retailers => new MockRetailerRepo(retailers);
        public IGenericRepository<Provider> Providers => throw new NotImplementedException();
        public IGenericRepository<OfferType> OfferTypes => throw new NotImplementedException();
        public IGenericRepository<Offer> Offers => throw new NotImplementedException();

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public void Dispose() { }
    }

    private class MockRetailerRepo(List<Retailer> retailers) : IGenericRepository<Retailer>
    {
        public Task<Retailer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Retailer?>(null);
        public Task<IEnumerable<Retailer>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<Retailer>>(retailers);
        public IQueryable<Retailer> GetQueryable(bool asNoTracking = true) => retailers.AsQueryable();
        public Task AddAsync(Retailer entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Update(Retailer entity) { }
        public void Delete(Retailer entity) { }
    }
}
