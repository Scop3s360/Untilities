using System.Diagnostics;
using AutoMapper;
using SaverSearch.Application.Common.Interfaces;
using SaverSearch.Application.Common.Mappings;
using SaverSearch.Application.Common.Models.Pipeline;
using SaverSearch.Application.Common.Models.Pipeline.Calculations;
using SaverSearch.Application.Dtos;
using SaverSearch.Application.Services.Pipeline;
using SaverSearch.Application.Services.Pipeline.Calculations;
using SaverSearch.Domain.Entities;
using Xunit;
using Xunit.Abstractions;

namespace SaverSearch.Tests.UnitTests;

public class SavingsCalculatorTests
{
    private readonly IMapper _mapper;
    private readonly ITestOutputHelper _output;
    private readonly List<ISavingsCalculationStrategy> _strategies;

    public SavingsCalculatorTests(ITestOutputHelper output)
    {
        _output = output;
        var config = new MapperConfiguration(
            cfg => cfg.AddProfile<MappingProfile>(),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance
        );
        _mapper = config.CreateMapper();

        _strategies = new List<ISavingsCalculationStrategy>
        {
            new PercentageCashbackStrategy(),
            new FixedCashbackStrategy(),
            new PercentageDiscountStrategy(),
            new FixedDiscountStrategy(),
            new RewardPointsStrategy(),
            new MortgageCashbackStrategy()
        };
    }

    private ResolvedOffer GetResolvedOffer(string title, decimal value, string valueType, decimal? maxReward = null, string? offerTypeName = "Cashback")
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
            Name = offerTypeName ?? "Cashback"
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
            MaximumReward = maxReward,
            OfferUrl = "https://offer.com",
            IsActive = true
        };

        var offerDto = _mapper.Map<OfferDto>(offer);
        var retailerDto = _mapper.Map<RetailerDto>(retailer);
        var providerDto = _mapper.Map<ProviderDto>(provider);
        var offerTypeDto = _mapper.Map<OfferTypeDto>(offerType);

        return new ResolvedOffer(offerDto, retailerDto, providerDto, offerTypeDto, OfferSource.Database, DateTime.UtcNow);
    }

    [Fact]
    public async Task PercentageCashback_ShouldCalculateSavingsCorrectly()
    {
        // Arrange
        var calculator = new SavingsCalculator(_strategies);
        var offer = GetResolvedOffer(title: "10% Cashback", value: 10.0m, valueType: "Percentage");
        var context = new DiscoveryContext(null, "Query", null, 150.0m, null, null, null, new Dictionary<string, string>());

        // Act
        var result = await calculator.CalculateAsync(offer, context);

        // Assert
        Assert.Equal(150.0m, result.TargetSpend);
        Assert.Equal(15.0m, result.RawSaving);
        Assert.Equal(15.0m, result.FinalSaving);
        Assert.Equal(10.0m, result.EffectiveSavingPercentage);
        Assert.False(result.CapApplied);
        Assert.Contains(result.CalculationSteps, s => s.Description.Contains("Potential Cashback"));
    }

    [Fact]
    public async Task PercentageDiscount_ShouldCapSavings_WhenMaxRewardIsSpecified()
    {
        // Arrange
        var calculator = new SavingsCalculator(_strategies);
        // 20% discount but capped at £15
        var offer = GetResolvedOffer(title: "20% Off", value: 20.0m, valueType: "Percentage", maxReward: 15.0m, offerTypeName: "Discount");
        // Spend £100 -> potential discount is £20, but capped at £15
        var context = new DiscoveryContext(null, "Query", null, 100.0m, null, null, null, new Dictionary<string, string>());

        // Act
        var result = await calculator.CalculateAsync(offer, context);

        // Assert
        Assert.Equal(20.0m, result.RawSaving);
        Assert.Equal(15.0m, result.FinalSaving);
        Assert.Equal(15.0m, result.EffectiveSavingPercentage); // Effective = 15/100 = 15%
        Assert.True(result.CapApplied);
        Assert.Contains(result.CalculationSteps, s => s.Description.Contains("Discount Cap Applied"));
    }

    [Fact]
    public async Task FixedCashback_ShouldApplyFixedAmountIndependentOfSpend()
    {
        // Arrange
        var calculator = new SavingsCalculator(_strategies);
        var offer = GetResolvedOffer(title: "£5 Cashback", value: 5.0m, valueType: "FixedAmount");
        var context = new DiscoveryContext(null, "Query", null, 80.0m, null, null, null, new Dictionary<string, string>());

        // Act
        var result = await calculator.CalculateAsync(offer, context);

        // Assert
        Assert.Equal(5.0m, result.FinalSaving);
        Assert.Equal(6.25m, result.EffectiveSavingPercentage); // 5/80 = 6.25%
    }

    [Fact]
    public async Task RewardPoints_ShouldConvertCorrectlyToCashValue()
    {
        // Arrange
        var calculator = new SavingsCalculator(_strategies);
        var offer = GetResolvedOffer(title: "3x Points Reward", value: 3.0m, valueType: "Points");
        var context = new DiscoveryContext(null, "Query", null, 100.0m, null, null, null, new Dictionary<string, string>());

        // Act
        var result = await calculator.CalculateAsync(offer, context);

        // Assert
        Assert.Equal(3.0m, result.FinalSaving); // 3x points on £100 = 300 pts. 300 * 0.01p = £3 cash value.
        Assert.Equal(3.0m, result.EffectiveSavingPercentage);
    }

    [Fact]
    public async Task PerformanceBenchmark_ShouldCompleteCalculationsEfficiently()
    {
        // Arrange
        var calculator = new SavingsCalculator(_strategies);
        var offer = GetResolvedOffer(title: "10% Cashback", value: 10.0m, valueType: "Percentage");
        var context = new DiscoveryContext(null, "Query", null, 250.0m, null, null, null, new Dictionary<string, string>());

        // Warmup
        await calculator.CalculateAsync(offer, context);

        // Act & Measure
        var stopwatch = Stopwatch.StartNew();
        var tasks = Enumerable.Range(1, 100000).Select(_ => calculator.CalculateAsync(offer, context)).ToList();
        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        _output.WriteLine($"Completed 100,000 stateless calculations concurrently in {stopwatch.ElapsedMilliseconds}ms.");

        // Assert
        Assert.Equal(100000, results.Length);
        Assert.All(results, r => Assert.Equal(25.0m, r.FinalSaving));
    }
}
