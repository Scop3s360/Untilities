using System.Diagnostics;
using Microsoft.Extensions.Options;
using SaverSearch.Application.Common.Interfaces;
using SaverSearch.Application.Common.Models.Pipeline;
using SaverSearch.Application.Common.Models.Pipeline.Calculations;
using SaverSearch.Application.Common.Models.Pipeline.Normalisation;

namespace SaverSearch.Application.Services.Pipeline.Normalisation;

public class CashbackNormalisationStrategy(IOptions<NormalisationSettings> settings) : INormalisationStrategy
{
    private readonly NormalisationSettings _settings = settings.Value;
    public string StrategyName => "Cashback Normalisation Strategy";

    public bool CanNormalise(CalculatedOffer calculatedOffer)
    {
        return calculatedOffer.CalculationStrategy.Contains("Cashback", StringComparison.OrdinalIgnoreCase);
    }

    public Task<NormalisedOffer> NormaliseAsync(CalculatedOffer calculatedOffer, DiscoveryContext context, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var steps = new List<NormalisationStep>
        {
            new NormalisationStep($"Original Cashback Saving: {calculatedOffer.FinalSaving:C}", calculatedOffer.FinalSaving),
            new NormalisationStep($"Expected Cashback Value: {calculatedOffer.FinalSaving:C}", calculatedOffer.FinalSaving)
        };

        stopwatch.Stop();

        var diagnostics = new NormalisationDiagnostics(
            stopwatch.ElapsedMilliseconds,
            StrategyName,
            "Calculated Offer",
            new List<string>()
        );

        return Task.FromResult(new NormalisedOffer(
            calculatedOffer,
            calculatedOffer.FinalSaving,
            calculatedOffer.FinalSaving,
            calculatedOffer.FinalSaving,
            calculatedOffer.EffectiveSavingPercentage,
            _settings.CashbackConfidence,
            "Direct Mapping",
            StrategyName,
            steps,
            diagnostics
        ));
    }
}

public class DiscountNormalisationStrategy(IOptions<NormalisationSettings> settings) : INormalisationStrategy
{
    private readonly NormalisationSettings _settings = settings.Value;
    public string StrategyName => "Discount Normalisation Strategy";

    public bool CanNormalise(CalculatedOffer calculatedOffer)
    {
        return calculatedOffer.CalculationStrategy.Contains("Discount", StringComparison.OrdinalIgnoreCase);
    }

    public Task<NormalisedOffer> NormaliseAsync(CalculatedOffer calculatedOffer, DiscoveryContext context, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var steps = new List<NormalisationStep>
        {
            new NormalisationStep($"Original Discount Saving: {calculatedOffer.FinalSaving:C}", calculatedOffer.FinalSaving),
            new NormalisationStep($"Expected Discount Value: {calculatedOffer.FinalSaving:C}", calculatedOffer.FinalSaving)
        };

        stopwatch.Stop();

        var diagnostics = new NormalisationDiagnostics(
            stopwatch.ElapsedMilliseconds,
            StrategyName,
            "Calculated Offer",
            new List<string>()
        );

        return Task.FromResult(new NormalisedOffer(
            calculatedOffer,
            calculatedOffer.FinalSaving,
            calculatedOffer.FinalSaving,
            calculatedOffer.FinalSaving,
            calculatedOffer.EffectiveSavingPercentage,
            _settings.DiscountConfidence,
            "Direct Mapping",
            StrategyName,
            steps,
            diagnostics
        ));
    }
}

public class PointsNormalisationStrategy(IOptions<NormalisationSettings> settings) : INormalisationStrategy
{
    private readonly NormalisationSettings _settings = settings.Value;
    public string StrategyName => "Points Normalisation Strategy";

    public bool CanNormalise(CalculatedOffer calculatedOffer)
    {
        return calculatedOffer.CalculationStrategy.Contains("Points", StringComparison.OrdinalIgnoreCase);
    }

    public Task<NormalisedOffer> NormaliseAsync(CalculatedOffer calculatedOffer, DiscoveryContext context, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Points value conversions: Guaranteed = monetary points value, Expected = scaled with multiplier (e.g. 1.2x partner value), Max = high partner value (e.g. 1.5x Nectar/Avios booster value)
        var guaranteed = calculatedOffer.FinalSaving;
        var expected = calculatedOffer.FinalSaving * 1.2m;
        var maxPossible = calculatedOffer.FinalSaving * 1.5m;

        var steps = new List<NormalisationStep>
        {
            new NormalisationStep($"Guaranteed Monetary Points Value: {guaranteed:C}", guaranteed),
            new NormalisationStep("Apply Partner Valuation Multiplier (1.2x)", 1.2m),
            new NormalisationStep($"Expected Points Value: {expected:C}", expected),
            new NormalisationStep("Apply Boost Booster Multiplier (1.5x)", 1.5m),
            new NormalisationStep($"Maximum Possible Points Value: {maxPossible:C}", maxPossible)
        };

        stopwatch.Stop();

        var diagnostics = new NormalisationDiagnostics(
            stopwatch.ElapsedMilliseconds,
            StrategyName,
            "Points Ratio Calculation",
            new List<string>()
        );

        var effectivePercentage = calculatedOffer.TargetSpend > 0 ? (expected / calculatedOffer.TargetSpend) * 100.0m : 0.0m;

        return Task.FromResult(new NormalisedOffer(
            calculatedOffer,
            expected,
            guaranteed,
            maxPossible,
            effectivePercentage,
            _settings.PointsConfidence,
            "Valuation Boost Mapping",
            StrategyName,
            steps,
            diagnostics
        ));
    }
}

public class MortgageNormalisationStrategy(IOptions<NormalisationSettings> settings) : INormalisationStrategy
{
    private readonly NormalisationSettings _settings = settings.Value;
    public string StrategyName => "Mortgage Cashback Normalisation Strategy";

    public bool CanNormalise(CalculatedOffer calculatedOffer)
    {
        return calculatedOffer.CalculationStrategy.Contains("Mortgage", StringComparison.OrdinalIgnoreCase);
    }

    public Task<NormalisedOffer> NormaliseAsync(CalculatedOffer calculatedOffer, DiscoveryContext context, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var steps = new List<NormalisationStep>
        {
            new NormalisationStep($"Original Mortgage Cashback Saving: {calculatedOffer.FinalSaving:C}", calculatedOffer.FinalSaving),
            new NormalisationStep($"Expected Mortgage Cashback Value: {calculatedOffer.FinalSaving:C}", calculatedOffer.FinalSaving)
        };

        stopwatch.Stop();

        var diagnostics = new NormalisationDiagnostics(
            stopwatch.ElapsedMilliseconds,
            StrategyName,
            "Calculated Offer",
            new List<string>()
        );

        return Task.FromResult(new NormalisedOffer(
            calculatedOffer,
            calculatedOffer.FinalSaving,
            calculatedOffer.FinalSaving,
            calculatedOffer.FinalSaving,
            calculatedOffer.EffectiveSavingPercentage,
            _settings.MortgageConfidence,
            "Direct Mapping",
            StrategyName,
            steps,
            diagnostics
        ));
    }
}
