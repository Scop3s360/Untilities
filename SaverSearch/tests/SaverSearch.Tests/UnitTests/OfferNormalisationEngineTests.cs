using System.Diagnostics;
using AutoMapper;
using Microsoft.Extensions.Options;
using SaverSearch.Application.Common.Interfaces;
using SaverSearch.Application.Common.Mappings;
using SaverSearch.Application.Common.Models.Pipeline;
using SaverSearch.Application.Common.Models.Pipeline.Calculations;
using SaverSearch.Application.Common.Models.Pipeline.Normalisation;
using SaverSearch.Application.Dtos;
using SaverSearch.Application.Services.Pipeline;
using SaverSearch.Application.Services.Pipeline.Normalisation;
using SaverSearch.Domain.Entities;
using Xunit;
using Xunit.Abstractions;

namespace SaverSearch.Tests.UnitTests;

public class OfferNormalisationEngineTests
{
    private readonly IMapper _mapper;
    private readonly ITestOutputHelper _output;
    private readonly IOptions<NormalisationSettings> _settingsOptions;
    private readonly List<INormalisationStrategy> _strategies;

    public OfferNormalisationEngineTests(ITestOutputHelper output)
    {
        _output = output;
        
        var config = new MapperConfiguration(
            cfg => cfg.AddProfile<MappingProfile>(),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance
        );
        _mapper = config.CreateMapper();

        _settingsOptions = Options.Create(new NormalisationSettings
        {
            CashbackConfidence = 100.0,
            DiscountConfidence = 100.0,
            PointsConfidence = 85.0,
            MortgageConfidence = 100.0,
            DefaultConfidence = 80.0
        });

        _strategies = new List<INormalisationStrategy>
        {
            new CashbackNormalisationStrategy(_settingsOptions),
            new DiscountNormalisationStrategy(_settingsOptions),
            new PointsNormalisationStrategy(_settingsOptions),
            new MortgageNormalisationStrategy(_settingsOptions)
        };
    }

    private CalculatedOffer GetCalculatedOffer(string title, decimal value, string valueType, string strategyName, decimal calculatedSaving)
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
            Value = value,
            ValueType = Enum.Parse<OfferValueType>(valueType, true),
            OfferUrl = "https://offer.com",
            IsActive = true
        };

        var offerDto = _mapper.Map<OfferDto>(offer);
        var resolvedOffer = new ResolvedOffer(
            offerDto,
            _mapper.Map<RetailerDto>(retailer),
            _mapper.Map<ProviderDto>(provider),
            _mapper.Map<OfferTypeDto>(offerType),
            OfferSource.Database,
            DateTime.UtcNow
        );

        var steps = new List<CalculationStep> { new CalculationStep("Direct", calculatedSaving) };
        var calcDiagnostics = new CalculatedOfferDiagnostics(0, strategyName, new List<string>());

        return new CalculatedOffer(
            offerDto,
            100.0m,
            calculatedSaving,
            calculatedSaving,
            10.0m,
            null,
            false,
            strategyName,
            steps,
            100.0,
            calcDiagnostics
        );
    }

    [Fact]
    public async Task CashbackStrategy_ShouldNormaliseValuesCorrectly()
    {
        // Arrange
        var engine = new OfferNormalisationEngine(_strategies);
        var offer = GetCalculatedOffer("Cashback Bonus", 10.0m, "Percentage", "Percentage Cashback Strategy", 10.0m);
        var context = new DiscoveryContext(null, "Query", null, 100.0m, null, null, null, new Dictionary<string, string>());

        // Act
        var results = await engine.NormaliseOffersAsync(new List<CalculatedOffer> { offer }, context);
        var result = results.First();

        // Assert
        Assert.Equal(10.0m, result.GuaranteedMonetaryValue);
        Assert.Equal(10.0m, result.ExpectedMonetaryValue);
        Assert.Equal(10.0m, result.MaximumPossibleValue);
        Assert.Equal(100.0, result.ConfidenceScore);
        Assert.Contains(result.AuditTrail, s => s.Description.Contains("Original Cashback Saving"));
    }

    [Fact]
    public async Task PointsStrategy_ShouldBoostExpectedAndMaximumValues()
    {
        // Arrange
        var engine = new OfferNormalisationEngine(_strategies);
        // £5 baseline cash value of points
        var offer = GetCalculatedOffer("Points Booster", 5.0m, "Points", "Reward Points Strategy", 5.0m);
        var context = new DiscoveryContext(null, "Query", null, 100.0m, null, null, null, new Dictionary<string, string>());

        // Act
        var results = await engine.NormaliseOffersAsync(new List<CalculatedOffer> { offer }, context);
        var result = results.First();

        // Assert
        Assert.Equal(5.0m, result.GuaranteedMonetaryValue); // £5 guaranteed cash value
        Assert.Equal(6.0m, result.ExpectedMonetaryValue); // 5 * 1.2 = £6 expected partner booster value
        Assert.Equal(7.50m, result.MaximumPossibleValue); // 5 * 1.5 = £7.50 max boost booster value
        Assert.Equal(85.0, result.ConfidenceScore);
        Assert.Contains(result.AuditTrail, s => s.Description.Contains("Partner Valuation Multiplier"));
    }

    [Fact]
    public async Task PerformanceBenchmark_ShouldScaleUnderConcurrentStress()
    {
        // Arrange
        var engine = new OfferNormalisationEngine(_strategies);
        var offer = GetCalculatedOffer("Cashback Bonus", 10.0m, "Percentage", "Percentage Cashback Strategy", 10.0m);
        var context = new DiscoveryContext(null, "Query", null, 100.0m, null, null, null, new Dictionary<string, string>());

        // Seed 100,000 calculated offers
        var list = Enumerable.Range(1, 100000).Select(_ => offer).ToList();

        // Act & Measure
        var stopwatch = Stopwatch.StartNew();
        var results = await engine.NormaliseOffersAsync(list, context);
        stopwatch.Stop();

        _output.WriteLine($"Processed 100,000 normalisations concurrently in {stopwatch.ElapsedMilliseconds}ms.");

        // Assert
        Assert.Equal(100000, results.Count());
        Assert.True(stopwatch.ElapsedMilliseconds < 500, "Performance stress exceeds 500ms budget limit.");
    }
}
