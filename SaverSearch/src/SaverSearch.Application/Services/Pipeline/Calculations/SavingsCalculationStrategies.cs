using System.Diagnostics;
using SaverSearch.Application.Common.Interfaces;
using SaverSearch.Application.Common.Models.Pipeline;
using SaverSearch.Application.Common.Models.Pipeline.Calculations;
using SaverSearch.Application.Dtos;
using SaverSearch.Domain.Entities;

namespace SaverSearch.Application.Services.Pipeline.Calculations;

public class PercentageCashbackStrategy : ISavingsCalculationStrategy
{
    public string StrategyName => "Percentage Cashback Strategy";

    public bool CanCalculate(OfferDto offer)
    {
        return offer.ValueType == OfferValueType.Percentage &&
               offer.Title.Contains("Cashback", StringComparison.OrdinalIgnoreCase);
    }

    public Task<CalculatedOffer> CalculateAsync(ResolvedOffer resolvedOffer, DiscoveryContext context, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var offer = resolvedOffer.Offer;
        var steps = new List<CalculationStep>();

        steps.Add(new CalculationStep($"Target Spend: {context.TargetSpend:C}", context.TargetSpend));
        steps.Add(new CalculationStep($"Cashback Rate: {offer.Value}%", offer.Value));

        var rawSaving = context.TargetSpend * (offer.Value / 100.0m);
        steps.Add(new CalculationStep($"Potential Cashback: {rawSaving:C}", rawSaving));

        var finalSaving = rawSaving;
        var capApplied = false;

        if (offer.MaximumReward.HasValue && rawSaving > offer.MaximumReward.Value)
        {
            finalSaving = offer.MaximumReward.Value;
            capApplied = true;
            steps.Add(new CalculationStep($"Maximum Cashback Cap Applied: {offer.MaximumReward.Value:C}", offer.MaximumReward.Value));
        }

        steps.Add(new CalculationStep($"Final Cashback Saving: {finalSaving:C}", finalSaving));
        stopwatch.Stop();

        var effectivePercentage = context.TargetSpend > 0 ? (finalSaving / context.TargetSpend) * 100.0m : 0.0m;

        var diagnostics = new CalculatedOfferDiagnostics(stopwatch.ElapsedMilliseconds, StrategyName, new List<string>());

        return Task.FromResult(new CalculatedOffer(
            offer,
            context.TargetSpend,
            rawSaving,
            finalSaving,
            effectivePercentage,
            offer.MaximumReward,
            capApplied,
            StrategyName,
            steps,
            100.0,
            diagnostics
        ));
    }
}

public class FixedCashbackStrategy : ISavingsCalculationStrategy
{
    public string StrategyName => "Fixed Cashback Strategy";

    public bool CanCalculate(OfferDto offer)
    {
        return offer.ValueType == OfferValueType.FixedAmount &&
               offer.Title.Contains("Cashback", StringComparison.OrdinalIgnoreCase);
    }

    public Task<CalculatedOffer> CalculateAsync(ResolvedOffer resolvedOffer, DiscoveryContext context, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var offer = resolvedOffer.Offer;
        var steps = new List<CalculationStep>();

        steps.Add(new CalculationStep($"Target Spend: {context.TargetSpend:C}", context.TargetSpend));
        steps.Add(new CalculationStep($"Fixed Cashback Reward: {offer.Value:C}", offer.Value));

        var rawSaving = offer.Value;
        var finalSaving = rawSaving;
        var capApplied = false;

        steps.Add(new CalculationStep($"Final Cashback Saving: {finalSaving:C}", finalSaving));
        stopwatch.Stop();

        var effectivePercentage = context.TargetSpend > 0 ? (finalSaving / context.TargetSpend) * 100.0m : 0.0m;
        var diagnostics = new CalculatedOfferDiagnostics(stopwatch.ElapsedMilliseconds, StrategyName, new List<string>());

        return Task.FromResult(new CalculatedOffer(
            offer,
            context.TargetSpend,
            rawSaving,
            finalSaving,
            effectivePercentage,
            offer.MaximumReward,
            capApplied,
            StrategyName,
            steps,
            100.0,
            diagnostics
        ));
    }
}

public class PercentageDiscountStrategy : ISavingsCalculationStrategy
{
    public string StrategyName => "Percentage Discount Strategy";

    public bool CanCalculate(OfferDto offer)
    {
        return offer.ValueType == OfferValueType.Percentage &&
               !offer.Title.Contains("Cashback", StringComparison.OrdinalIgnoreCase);
    }

    public Task<CalculatedOffer> CalculateAsync(ResolvedOffer resolvedOffer, DiscoveryContext context, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var offer = resolvedOffer.Offer;
        var steps = new List<CalculationStep>();

        steps.Add(new CalculationStep($"Target Spend: {context.TargetSpend:C}", context.TargetSpend));
        steps.Add(new CalculationStep($"Discount Rate: {offer.Value}%", offer.Value));

        var rawSaving = context.TargetSpend * (offer.Value / 100.0m);
        steps.Add(new CalculationStep($"Potential Discount: {rawSaving:C}", rawSaving));

        var finalSaving = rawSaving;
        var capApplied = false;

        if (offer.MaximumReward.HasValue && rawSaving > offer.MaximumReward.Value)
        {
            finalSaving = offer.MaximumReward.Value;
            capApplied = true;
            steps.Add(new CalculationStep($"Maximum Discount Cap Applied: {offer.MaximumReward.Value:C}", offer.MaximumReward.Value));
        }

        steps.Add(new CalculationStep($"Final Discount Saving: {finalSaving:C}", finalSaving));
        stopwatch.Stop();

        var effectivePercentage = context.TargetSpend > 0 ? (finalSaving / context.TargetSpend) * 100.0m : 0.0m;
        var diagnostics = new CalculatedOfferDiagnostics(stopwatch.ElapsedMilliseconds, StrategyName, new List<string>());

        return Task.FromResult(new CalculatedOffer(
            offer,
            context.TargetSpend,
            rawSaving,
            finalSaving,
            effectivePercentage,
            offer.MaximumReward,
            capApplied,
            StrategyName,
            steps,
            100.0,
            diagnostics
        ));
    }
}

public class FixedDiscountStrategy : ISavingsCalculationStrategy
{
    public string StrategyName => "Fixed Discount Strategy";

    public bool CanCalculate(OfferDto offer)
    {
        return offer.ValueType == OfferValueType.FixedAmount &&
               !offer.Title.Contains("Cashback", StringComparison.OrdinalIgnoreCase);
    }

    public Task<CalculatedOffer> CalculateAsync(ResolvedOffer resolvedOffer, DiscoveryContext context, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var offer = resolvedOffer.Offer;
        var steps = new List<CalculationStep>();

        steps.Add(new CalculationStep($"Target Spend: {context.TargetSpend:C}", context.TargetSpend));
        steps.Add(new CalculationStep($"Fixed Discount Amount: {offer.Value:C}", offer.Value));

        var rawSaving = offer.Value;
        var finalSaving = rawSaving;
        var capApplied = false;

        steps.Add(new CalculationStep($"Final Discount Saving: {finalSaving:C}", finalSaving));
        stopwatch.Stop();

        var effectivePercentage = context.TargetSpend > 0 ? (finalSaving / context.TargetSpend) * 100.0m : 0.0m;
        var diagnostics = new CalculatedOfferDiagnostics(stopwatch.ElapsedMilliseconds, StrategyName, new List<string>());

        return Task.FromResult(new CalculatedOffer(
            offer,
            context.TargetSpend,
            rawSaving,
            finalSaving,
            effectivePercentage,
            offer.MaximumReward,
            capApplied,
            StrategyName,
            steps,
            100.0,
            diagnostics
        ));
    }
}

public class RewardPointsStrategy : ISavingsCalculationStrategy
{
    public string StrategyName => "Reward Points Strategy";

    public bool CanCalculate(OfferDto offer)
    {
        return offer.ValueType == OfferValueType.Points;
    }

    public Task<CalculatedOffer> CalculateAsync(ResolvedOffer resolvedOffer, DiscoveryContext context, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var offer = resolvedOffer.Offer;
        var steps = new List<CalculationStep>();

        steps.Add(new CalculationStep($"Target Spend: {context.TargetSpend:C}", context.TargetSpend));
        steps.Add(new CalculationStep($"Reward Points Multiplier: {offer.Value}x", offer.Value));

        // Let's assume standard value: 1 point = £0.01 (1p) cash equivalent value
        var pointsPotential = context.TargetSpend * offer.Value;
        var pointsValue = pointsPotential * 0.01m;

        steps.Add(new CalculationStep($"Potential Points Earned: {pointsPotential} pts", pointsPotential));
        steps.Add(new CalculationStep($"Points Monetary Cash Value: {pointsValue:C}", pointsValue));

        var rawSaving = pointsValue;
        var finalSaving = rawSaving;
        var capApplied = false;

        steps.Add(new CalculationStep($"Final Points Saving: {finalSaving:C}", finalSaving));
        stopwatch.Stop();

        var effectivePercentage = context.TargetSpend > 0 ? (finalSaving / context.TargetSpend) * 100.0m : 0.0m;
        var diagnostics = new CalculatedOfferDiagnostics(stopwatch.ElapsedMilliseconds, StrategyName, new List<string>());

        return Task.FromResult(new CalculatedOffer(
            offer,
            context.TargetSpend,
            rawSaving,
            finalSaving,
            effectivePercentage,
            offer.MaximumReward,
            capApplied,
            StrategyName,
            steps,
            100.0,
            diagnostics
        ));
    }
}

public class MortgageCashbackStrategy : ISavingsCalculationStrategy
{
    public string StrategyName => "Mortgage Cashback Strategy";

    public bool CanCalculate(OfferDto offer)
    {
        return offer.Title.Contains("Mortgage", StringComparison.OrdinalIgnoreCase);
    }

    public Task<CalculatedOffer> CalculateAsync(ResolvedOffer resolvedOffer, DiscoveryContext context, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var offer = resolvedOffer.Offer;
        var steps = new List<CalculationStep>();

        steps.Add(new CalculationStep($"Fixed Mortgage Cashback Value: {offer.Value:C}", offer.Value));

        var rawSaving = offer.Value;
        var finalSaving = rawSaving;
        var capApplied = false;

        steps.Add(new CalculationStep($"Final Mortgage Cashback Saving: {finalSaving:C}", finalSaving));
        stopwatch.Stop();

        var effectivePercentage = context.TargetSpend > 0 ? (finalSaving / context.TargetSpend) * 100.0m : 0.0m;
        var diagnostics = new CalculatedOfferDiagnostics(stopwatch.ElapsedMilliseconds, StrategyName, new List<string>());

        return Task.FromResult(new CalculatedOffer(
            offer,
            context.TargetSpend,
            rawSaving,
            finalSaving,
            effectivePercentage,
            offer.MaximumReward,
            capApplied,
            StrategyName,
            steps,
            100.0,
            diagnostics
        ));
    }
}
