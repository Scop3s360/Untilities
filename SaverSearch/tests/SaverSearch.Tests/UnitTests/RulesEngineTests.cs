using AutoMapper;
using SaverSearch.Application.Common.Interfaces;
using SaverSearch.Application.Common.Mappings;
using SaverSearch.Application.Common.Models.Pipeline;
using SaverSearch.Application.Common.Models.Pipeline.Rules;
using SaverSearch.Application.Dtos;
using SaverSearch.Application.Services.Pipeline;
using SaverSearch.Application.Services.Pipeline.Rules;
using SaverSearch.Domain.Entities;
using Xunit;

namespace SaverSearch.Tests.UnitTests;

public class RulesEngineTests
{
    private readonly List<IRuleEvaluator> _evaluators;
    private readonly IMapper _mapper;

    public RulesEngineTests()
    {
        _evaluators = new List<IRuleEvaluator>
        {
            new MinimumSpendRule(),
            new OfferDateRule(),
            new PaymentMethodRule(),
            new RegionRule()
        };

        var config = new MapperConfiguration(
            cfg => cfg.AddProfile<MappingProfile>(),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance
        );
        _mapper = config.CreateMapper();
    }

    private ResolvedOffer GetMockOffer(decimal minSpend = 0.0m, string? terms = null, string? title = null, DateTime? startDate = null, DateTime? endDate = null)
    {
        var category = new Category { Id = Guid.NewGuid(), Name = "Category" };
        var retailer = new Retailer
        {
            Id = Guid.NewGuid(),
            Name = "Retailer",
            Slug = "retailer",
            Website = "https://retailer.com",
            CategoryId = category.Id,
            Category = category
        };
        var provider = new Provider
        {
            Id = Guid.NewGuid(),
            Name = "Provider",
            Website = "https://provider.com"
        };
        var offerType = new OfferType
        {
            Id = Guid.NewGuid(),
            Name = "Cashback"
        };
        
        var offer = new Offer
        {
            Id = Guid.NewGuid(),
            RetailerId = retailer.Id,
            Retailer = retailer,
            ProviderId = provider.Id,
            Provider = provider,
            OfferTypeId = offerType.Id,
            OfferType = offerType,
            Title = title ?? "Test Offer",
            Value = 5.0m,
            ValueType = OfferValueType.Percentage,
            MinimumSpend = minSpend,
            StartDate = startDate,
            EndDate = endDate,
            Terms = terms,
            OfferUrl = "https://deals.com",
            IsActive = true
        };

        var offerDto = _mapper.Map<OfferDto>(offer);
        var retailerDto = _mapper.Map<RetailerDto>(retailer);
        var providerDto = _mapper.Map<ProviderDto>(provider);
        var offerTypeDto = _mapper.Map<OfferTypeDto>(offerType);

        return new ResolvedOffer(offerDto, retailerDto, providerDto, offerTypeDto, OfferSource.Database, DateTime.UtcNow);
    }

    [Fact]
    public async Task MinimumSpendRule_ShouldFail_WhenSpendIsLess()
    {
        // Arrange
        var rule = new MinimumSpendRule();
        var offer = GetMockOffer(minSpend: 50.0m);
        var context = new DiscoveryContext(null, "Query", null, 40.0m, null, null, null, new Dictionary<string, string>());

        // Act
        var result = await rule.EvaluateAsync(offer, context);

        // Assert
        Assert.False(result.Passed);
        Assert.Equal("Critical", result.Severity);
        Assert.Contains("less than minimum required", result.FailureReason);
    }

    [Fact]
    public async Task MinimumSpendRule_ShouldPass_WhenSpendIsExactOrGreater()
    {
        // Arrange
        var rule = new MinimumSpendRule();
        var offer = GetMockOffer(minSpend: 50.0m);
        var contextExact = new DiscoveryContext(null, "Query", null, 50.0m, null, null, null, new Dictionary<string, string>());
        var contextGreater = new DiscoveryContext(null, "Query", null, 60.0m, null, null, null, new Dictionary<string, string>());

        // Act & Assert
        var resultExact = await rule.EvaluateAsync(offer, contextExact);
        var resultGreater = await rule.EvaluateAsync(offer, contextGreater);

        Assert.True(resultExact.Passed);
        Assert.True(resultGreater.Passed);
    }

    [Fact]
    public async Task PaymentMethodRule_ShouldFail_WhenCardRequiredDoesNotMatch()
    {
        // Arrange
        var rule = new PaymentMethodRule();
        var offer = GetMockOffer(terms: "Barclaycard required for this transaction.");
        var context = new DiscoveryContext(null, "Query", null, 10.0m, "Chase Credit Card", null, null, new Dictionary<string, string>());

        // Act
        var result = await rule.EvaluateAsync(offer, context);

        // Assert
        Assert.False(result.Passed);
        Assert.Equal("Critical", result.Severity);
        Assert.Contains("requires payment with a Barclaycard", result.FailureReason);
    }

    [Fact]
    public async Task RegionRule_ShouldFail_WhenRegionIsRestricted()
    {
        // Arrange
        var rule = new RegionRule();
        var offer = GetMockOffer(terms: "UK only offer.");
        var context = new DiscoveryContext(null, "Query", null, 10.0m, null, null, "USA", new Dictionary<string, string>());

        // Act
        var result = await rule.EvaluateAsync(offer, context);

        // Assert
        Assert.False(result.Passed);
        Assert.Contains("restricted to UK", result.FailureReason);
    }

    [Fact]
    public async Task RulesEngine_ShouldAggregateMultipleRuleResults()
    {
        // Arrange
        var engine = new RulesEngine(_evaluators);
        // Minimum spend 30 required, but user spends 40 (passes spend, but fails region because it's UK only and user is in USA)
        var offer = GetMockOffer(minSpend: 30.0m, terms: "UK only offer.");
        var context = new DiscoveryContext(null, "Query", null, 40.0m, null, null, "USA", new Dictionary<string, string>());

        // Act
        var result = await engine.EvaluateAsync(offer, context);

        // Assert
        Assert.False(result.IsEligible); // Failed region check (which is Critical)
        Assert.Equal(4, result.RulesEvaluated.Count());
        Assert.Equal(3, result.PassedRules.Count());
        Assert.Single(result.FailedRules);
        Assert.Equal("Geographical Region Rule", result.FailedRules.First().RuleName);
    }

    [Fact]
    public async Task RulesEngine_ShouldSupportCancellation()
    {
        // Arrange
        var engine = new RulesEngine(_evaluators);
        var offer = GetMockOffer();
        var context = new DiscoveryContext(null, "Query", null, 10.0m, null, null, null, new Dictionary<string, string>());

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await engine.EvaluateAsync(offer, context, cts.Token);

        // Assert
        Assert.False(result.IsEligible);
        Assert.All(result.RulesEvaluated, r => Assert.False(r.Passed));
    }
}
