using System.Diagnostics;
using AutoMapper;
using SaverSearch.Application.Common.Interfaces;
using SaverSearch.Application.Common.Mappings;
using SaverSearch.Application.Common.Models.Pipeline;
using SaverSearch.Application.Common.Models.Pipeline.Calculations;
using SaverSearch.Application.Common.Models.Pipeline.Normalisation;
using SaverSearch.Application.Common.Models.Pipeline.Planning;
using SaverSearch.Application.Common.Models.Pipeline.Ranking;
using SaverSearch.Application.Dtos;
using SaverSearch.Application.Services.Pipeline;
using SaverSearch.Application.Services.Pipeline.Planning;
using SaverSearch.Domain.Entities;
using Xunit;
using Xunit.Abstractions;

namespace SaverSearch.Tests.UnitTests;

public class PurchasePlanningEngineTests
{
    private readonly IMapper _mapper;
    private readonly ITestOutputHelper _output;
    private readonly List<ICompatibilityEvaluator> _evaluators;
    private readonly List<IPurchasePlanningStrategy> _strategies;

    public PurchasePlanningEngineTests(ITestOutputHelper output)
    {
        _output = output;

        var config = new MapperConfiguration(
            cfg => cfg.AddProfile<MappingProfile>(),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance
        );
        _mapper = config.CreateMapper();

        _evaluators = new List<ICompatibilityEvaluator>
        {
            new ProviderCompatibilityEvaluator(),
            new OfferTypeCompatibilityEvaluator()
        };

        _strategies = new List<IPurchasePlanningStrategy>
        {
            new MaximumSavingStrategy(_evaluators),
            new LowestComplexityStrategy(_evaluators),
            new BalancedPlanningStrategy(_evaluators)
        };
    }

    private RankedOffer GetMockRankedOffer(string title, decimal expectedVal, Guid providerId, string? terms = null)
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
            Id = providerId,
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
            Title = title,
            Value = expectedVal,
            ValueType = OfferValueType.Percentage,
            OfferUrl = "https://offer.com",
            Terms = terms,
            IsActive = true
        };

        var offerDto = _mapper.Map<OfferDto>(offer);

        var steps = new List<CalculationStep> { new CalculationStep("Direct", expectedVal) };
        var calcDiagnostics = new CalculatedOfferDiagnostics(0, "Percentage Cashback Strategy", new List<string>());

        var calculated = new CalculatedOffer(
            offerDto,
            _mapper.Map<RetailerDto>(retailer),
            _mapper.Map<ProviderDto>(provider),
            _mapper.Map<OfferTypeDto>(offerType),
            100.0m,
            expectedVal,
            expectedVal,
            10.0m,
            null,
            false,
            "Percentage Cashback Strategy",
            steps,
            100.0,
            calcDiagnostics
        );

        var normSteps = new List<NormalisationStep> { new NormalisationStep("Mapping", expectedVal) };
        var normDiagnostics = new NormalisationDiagnostics(0, "Cashback Normalisation Strategy", "Direct", new List<string>());

        var normalised = new NormalisedOffer(
            calculated,
            expectedVal,
            expectedVal,
            expectedVal,
            10.0m,
            100.0,
            "Direct",
            "Cashback Normalisation Strategy",
            normSteps,
            normDiagnostics
        );

        var scoringFactors = new List<ScoringFactorResult> { new ScoringFactorResult("Mock Factor", (double)expectedVal, (double)expectedVal) };
        var rankDiagnostics = new RankedOfferDiagnostics(0, "Mock Strategy", scoringFactors, new List<string>());

        return new RankedOffer(
            normalised,
            (double)expectedVal,
            scoringFactors,
            1,
            "Mock Strategy",
            "Mock Explanation",
            rankDiagnostics
        );
    }

    [Fact]
    public async Task ProviderCompatibilityEvaluator_ShouldBlockSameProviderNonStackableOffers()
    {
        // Arrange
        var evaluator = new ProviderCompatibilityEvaluator();
        var providerId = Guid.NewGuid();
        var offer1 = GetMockRankedOffer("First Cashback", 5.0m, providerId);
        var offer2 = GetMockRankedOffer("Second Cashback", 10.0m, providerId);

        var context = new DiscoveryContext(null, "Query", null, 100.0m, null, null, null, new Dictionary<string, string>());

        // Act
        var result = await evaluator.EvaluateCompatibilityAsync(offer1, offer2, context);

        // Assert
        Assert.Equal(CompatibilityResult.Incompatible, result.Result);
        Assert.Contains("Cannot stack multiple offers from the same provider", result.Explanation);
    }

    [Fact]
    public async Task StackingEngine_ShouldSupportStackingMultipleCompatibleOffers()
    {
        // Arrange
        var engine = new PurchasePlanningEngine(_strategies);
        var provider1 = Guid.NewGuid();
        var provider2 = Guid.NewGuid();
        
        // A gift card and a cashback portal offer from different providers are highly compatible
        var giftCard = GetMockRankedOffer("Tesco Gift Card", 4.0m, provider1);
        var cashback = GetMockRankedOffer("Tesco Cashback Portal", 5.0m, provider2);

        var context = new DiscoveryContext(null, "Query", null, 100.0m, null, null, null, new Dictionary<string, string>());

        // Act
        var plans = (await engine.PlanPurchasesAsync(new List<RankedOffer> { giftCard, cashback }, context)).ToList();
        var plan = plans.First();

        // Assert
        Assert.Equal(2, plan.IncludedOffers.Count);
        Assert.Equal(9.0m, plan.TotalExpectedSaving);
        Assert.Contains(plan.PurchasePath, s => s.ActionDescription.Contains("Gift Card"));
        Assert.Contains(plan.PurchasePath, s => s.ActionDescription.Contains("portal"));
    }

    [Fact]
    public async Task PerformanceBenchmark_ShouldEvaluateCombinationsFast()
    {
        // Arrange
        var engine = new PurchasePlanningEngine(_strategies);
        var providerId = Guid.NewGuid();
        var offer = GetMockRankedOffer("Tesco Cashback Portal", 5.0m, providerId);
        var context = new DiscoveryContext(null, "Query", null, 100.0m, null, null, null, new Dictionary<string, string>());

        // Generate 100 offers to trigger 100 x 100 combination evaluations (10,000 matches)
        var list = Enumerable.Range(1, 100).Select(i => GetMockRankedOffer($"Offer #{i}", 5.0m, Guid.NewGuid())).ToList();

        // Act & Measure
        var stopwatch = Stopwatch.StartNew();
        var plans = await engine.PlanPurchasesAsync(list, context);
        stopwatch.Stop();

        _output.WriteLine($"Evaluated compatible plans for 100 offers in {stopwatch.ElapsedMilliseconds}ms.");

        // Assert
        Assert.Single(plans);
        Assert.True(stopwatch.ElapsedMilliseconds < 500, "Performance stress exceeds 500ms budget limit.");
    }
}
