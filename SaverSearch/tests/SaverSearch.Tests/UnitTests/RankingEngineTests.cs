using System.Diagnostics;
using AutoMapper;
using Microsoft.Extensions.Options;
using SaverSearch.Application.Common.Interfaces;
using SaverSearch.Application.Common.Mappings;
using SaverSearch.Application.Common.Models.Pipeline;
using SaverSearch.Application.Common.Models.Pipeline.Calculations;
using SaverSearch.Application.Common.Models.Pipeline.Normalisation;
using SaverSearch.Application.Common.Models.Pipeline.Ranking;
using SaverSearch.Application.Dtos;
using SaverSearch.Application.Services.Pipeline;
using SaverSearch.Application.Services.Pipeline.Ranking;
using SaverSearch.Domain.Entities;
using Xunit;
using Xunit.Abstractions;

namespace SaverSearch.Tests.UnitTests;

public class RankingEngineTests
{
    private readonly IMapper _mapper;
    private readonly ITestOutputHelper _output;
    private readonly IOptions<RankingSettings> _settingsOptions;
    private readonly List<IRankingStrategy> _strategies;
    private readonly List<IScoringFactor> _scoringFactors;

    public RankingEngineTests(ITestOutputHelper output)
    {
        _output = output;

        var config = new MapperConfiguration(
            cfg => cfg.AddProfile<MappingProfile>(),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance
        );
        _mapper = config.CreateMapper();

        _settingsOptions = Options.Create(new RankingSettings
        {
            ExpectedValueWeight = 0.40,
            GuaranteedValueWeight = 0.20,
            ConfidenceWeight = 0.20,
            ComplexityWeight = 0.20
        });

        _scoringFactors = new List<IScoringFactor>
        {
            new ExpectedValueFactor(),
            new GuaranteedValueFactor(),
            new ConfidenceScoreFactor(),
            new ComplexityScoreFactor()
        };

        _strategies = new List<IRankingStrategy>
        {
            new BestMonetaryValueStrategy(),
            new HighestConfidenceStrategy(),
            new LowestComplexityStrategy(),
            new BalancedRankingStrategy(_settingsOptions, _scoringFactors)
        };
    }

    private NormalisedOffer GetNormalisedOffer(string title, decimal expectedVal, decimal guaranteedVal, double confidence, string? terms = null)
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

        return new NormalisedOffer(
            calculated,
            expectedVal,
            guaranteedVal,
            expectedVal,
            10.0m,
            confidence,
            "Direct",
            "Cashback Normalisation Strategy",
            normSteps,
            normDiagnostics
        );
    }

    [Fact]
    public async Task BestMonetaryValueStrategy_ShouldRankByExpectedValue()
    {
        // Arrange
        var engine = new RankingEngine(_strategies);
        var offer1 = GetNormalisedOffer("Offer Low", 5.0m, 5.0m, 100.0);
        var offer2 = GetNormalisedOffer("Offer High", 20.0m, 20.0m, 90.0);

        var context = new DiscoveryContext(null, "Query", null, 100.0m, null, null, null, new Dictionary<string, string>
        {
            { "RankingStrategy", "Monetary" }
        });

        // Act
        var ranked = await engine.RankOffersAsync(new List<NormalisedOffer> { offer1, offer2 }, context);

        // Assert
        Assert.Equal("Offer High", ranked.First().NormalisedOffer.CalculatedOffer.Offer.Title);
        Assert.Equal(1, ranked.First().RankingPosition);
    }

    [Fact]
    public async Task LowestComplexityStrategy_ShouldRankDirectHigherThanGiftCard()
    {
        // Arrange
        var engine = new RankingEngine(_strategies);
        var directOffer = GetNormalisedOffer("Direct Offer", 10.0m, 10.0m, 100.0, terms: "Direct payment.");
        var giftCardOffer = GetNormalisedOffer("Gift Card Offer", 10.0m, 10.0m, 100.0, terms: "Gift card purchase required.");

        var context = new DiscoveryContext(null, "Query", null, 100.0m, null, null, null, new Dictionary<string, string>
        {
            { "RankingStrategy", "Complexity" }
        });

        // Act
        var ranked = await engine.RankOffersAsync(new List<NormalisedOffer> { giftCardOffer, directOffer }, context);

        // Assert
        Assert.Equal("Direct Offer", ranked.First().NormalisedOffer.CalculatedOffer.Offer.Title);
    }

    [Fact]
    public async Task BalancedRankingStrategy_ShouldSortByWeightedFactors()
    {
        // Arrange
        var engine = new RankingEngine(_strategies);
        var offer1 = GetNormalisedOffer("Low Val High Confidence", 10.0m, 10.0m, 100.0);
        var offer2 = GetNormalisedOffer("High Val Low Confidence", 30.0m, 30.0m, 50.0);

        var context = new DiscoveryContext(null, "Query", null, 100.0m, null, null, null, new Dictionary<string, string>()); // Balanced strategy defaults

        // Act
        var ranked = await engine.RankOffersAsync(new List<NormalisedOffer> { offer1, offer2 }, context);

        // Assert
        // Expected Value weights 0.40, Confidence weights 0.20, Guaranteed value weights 0.20, Complexity weights 0.20
        // Offer1 score: (10 * 0.40) + (10 * 0.20) + (100 * 0.20) + (100 * 0.20) = 4 + 2 + 20 + 20 = 46.0
        // Offer2 score: (30 * 0.40) + (30 * 0.20) + (50 * 0.20) + (100 * 0.20) = 12 + 6 + 10 + 20 = 48.0
        Assert.Equal("High Val Low Confidence", ranked.First().NormalisedOffer.CalculatedOffer.Offer.Title);
    }

    [Fact]
    public async Task PerformanceBenchmark_ShouldRankLargeVolumeUnderStress()
    {
        // Arrange
        var engine = new RankingEngine(_strategies);
        var offer = GetNormalisedOffer("Balanced Offer", 10.0m, 10.0m, 95.0);
        var context = new DiscoveryContext(null, "Query", null, 100.0m, null, null, null, new Dictionary<string, string>());

        // Seed 100,000 normalised offers
        var list = Enumerable.Range(1, 100000).Select(_ => offer).ToList();

        // Act & Measure
        var stopwatch = Stopwatch.StartNew();
        var ranked = await engine.RankOffersAsync(list, context);
        stopwatch.Stop();

        _output.WriteLine($"Ranked 100,000 offers concurrently in {stopwatch.ElapsedMilliseconds}ms.");

        // Assert
        Assert.Equal(100000, ranked.Count());
        Assert.True(stopwatch.ElapsedMilliseconds < 500, "Performance stress exceeds 500ms budget limit.");
    }
}
