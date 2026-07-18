using System.Diagnostics;
using SaverSearch.Application.Common.Interfaces;
using SaverSearch.Application.Common.Models.Pipeline;
using SaverSearch.Application.Common.Models.Pipeline.Planning;
using SaverSearch.Application.Common.Models.Pipeline.Ranking;

namespace SaverSearch.Application.Services.Pipeline.Planning;

public abstract class BasePlanningStrategy(IEnumerable<ICompatibilityEvaluator> evaluators)
{
    protected readonly List<ICompatibilityEvaluator> Evaluators = evaluators.ToList();

    protected async Task<List<CompatibilityEvidence>> EvaluateOfferListCompatibilityAsync(
        List<RankedOffer> offers, 
        DiscoveryContext context, 
        CancellationToken cancellationToken)
    {
        var evidences = new List<CompatibilityEvidence>();

        for (int i = 0; i < offers.Count; i++)
        {
            for (int j = i + 1; j < offers.Count; j++)
            {
                foreach (var evaluator in Evaluators)
                {
                    var evidence = await evaluator.EvaluateCompatibilityAsync(offers[i], offers[j], context, cancellationToken);
                    evidences.Add(evidence);
                }
            }
        }

        return evidences;
    }

    protected List<PurchasePathStep> BuildPurchasePath(List<RankedOffer> offers)
    {
        var steps = new List<PurchasePathStep>();
        int stepNum = 1;

        // 1. Gift card step
        var giftCards = offers.Where(o => o.NormalisedOffer.CalculatedOffer.Offer.Title.Contains("Gift Card", StringComparison.OrdinalIgnoreCase)).ToList();
        foreach (var gc in giftCards)
        {
            steps.Add(new PurchasePathStep(
                stepNum++,
                $"Purchase a discounted Gift Card for {gc.NormalisedOffer.CalculatedOffer.Offer.Title}.",
                "Buy this voucher/gift card in advance to secure initial savings before checking out."
            ));
        }

        // 2. Portal/Cashback step
        var cashbacks = offers.Where(o => !giftCards.Contains(o) && o.NormalisedOffer.CalculatedOffer.Offer.Title.Contains("Cashback", StringComparison.OrdinalIgnoreCase)).ToList();
        foreach (var cb in cashbacks)
        {
            steps.Add(new PurchasePathStep(
                stepNum++,
                $"Navigate to the retailer site using the {cb.NormalisedOffer.CalculatedOffer.Offer.Title} portal.",
                "Ensure your browser cookies are enabled to track portal rewards correctly."
            ));
        }

        // 3. Promo code/Discount step
        var discounts = offers.Where(o => !giftCards.Contains(o) && !cashbacks.Contains(o)).ToList();
        foreach (var dc in discounts)
        {
            steps.Add(new PurchasePathStep(
                stepNum++,
                $"Apply the promotional coupon/discount code: '{dc.NormalisedOffer.CalculatedOffer.Offer.Title}'.",
                "Enter this voucher code inside the checkout basket during ordering."
            ));
        }

        return steps;
    }
}

public class MaximumSavingStrategy(IEnumerable<ICompatibilityEvaluator> evaluators) 
    : BasePlanningStrategy(evaluators), IPurchasePlanningStrategy
{
    public string StrategyName => "Maximum Saving Strategy";

    public bool CanPlan(DiscoveryContext context)
    {
        return context.Preferences.TryGetValue("PlanningStrategy", out var strategy) &&
               strategy.Equals("MaxSaving", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<IEnumerable<PurchasePlan>> PlanPurchasesAsync(IEnumerable<RankedOffer> rankedOffers, DiscoveryContext context, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var offersList = rankedOffers.OrderByDescending(o => o.NormalisedOffer.ExpectedMonetaryValue).ToList();
        
        var compatibleOffers = new List<RankedOffer>();
        var evidences = new List<CompatibilityEvidence>();

        foreach (var offer in offersList)
        {
            var isCompatible = true;
            foreach (var existing in compatibleOffers)
            {
                foreach (var evaluator in Evaluators)
                {
                    var evidence = await evaluator.EvaluateCompatibilityAsync(existing, offer, context, cancellationToken);
                    evidences.Add(evidence);

                    if (evidence.Result == CompatibilityResult.Incompatible)
                    {
                        isCompatible = false;
                        break;
                    }
                }
                if (!isCompatible) break;
            }

            if (isCompatible)
            {
                compatibleOffers.Add(offer);
            }
        }

        var path = BuildPurchasePath(compatibleOffers);
        var totalGuaranteed = compatibleOffers.Sum(o => o.NormalisedOffer.GuaranteedMonetaryValue);
        var totalExpected = compatibleOffers.Sum(o => o.NormalisedOffer.ExpectedMonetaryValue);
        var maxPossible = compatibleOffers.Sum(o => o.NormalisedOffer.MaximumPossibleValue);

        stopwatch.Stop();

        var diagnostics = new PurchasePlanDiagnostics(
            stopwatch.ElapsedMilliseconds,
            StrategyName,
            evidences.Count,
            new List<string>()
        );

        var plan = new PurchasePlan(
            path,
            compatibleOffers,
            totalGuaranteed,
            totalExpected,
            maxPossible,
            90.0,
            20.0,
            compatibleOffers.Select(o => $"Claim {o.NormalisedOffer.CalculatedOffer.Offer.Title}").ToList(),
            compatibleOffers.Select(o => o.NormalisedOffer.CalculatedOffer.Provider.Name).Distinct().ToList(),
            evidences,
            "Optimized to deliver the absolute highest expected monetary value return.",
            diagnostics
        );

        return new List<PurchasePlan> { plan };
    }
}

public class LowestComplexityStrategy(IEnumerable<ICompatibilityEvaluator> evaluators) 
    : BasePlanningStrategy(evaluators), IPurchasePlanningStrategy
{
    public string StrategyName => "Lowest Complexity Strategy";

    public bool CanPlan(DiscoveryContext context)
    {
        return context.Preferences.TryGetValue("PlanningStrategy", out var strategy) &&
               strategy.Equals("LowComplexity", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<IEnumerable<PurchasePlan>> PlanPurchasesAsync(IEnumerable<RankedOffer> rankedOffers, DiscoveryContext context, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Simplicity check: Complexity score is mapped to EstimatedUserEffort. We sort by lowest estimated effort.
        var offersList = rankedOffers.OrderBy(o => o.NormalisedOffer.CalculatedOffer.FinalSaving).ToList(); // simpler first
        
        var compatibleOffers = new List<RankedOffer>();
        var evidences = new List<CompatibilityEvidence>();

        foreach (var offer in offersList)
        {
            var isCompatible = true;
            foreach (var existing in compatibleOffers)
            {
                foreach (var evaluator in Evaluators)
                {
                    var evidence = await evaluator.EvaluateCompatibilityAsync(existing, offer, context, cancellationToken);
                    evidences.Add(evidence);

                    if (evidence.Result == CompatibilityResult.Incompatible)
                    {
                        isCompatible = false;
                        break;
                    }
                }
                if (!isCompatible) break;
            }

            if (isCompatible)
            {
                compatibleOffers.Add(offer);
            }
        }

        var path = BuildPurchasePath(compatibleOffers);
        var totalGuaranteed = compatibleOffers.Sum(o => o.NormalisedOffer.GuaranteedMonetaryValue);
        var totalExpected = compatibleOffers.Sum(o => o.NormalisedOffer.ExpectedMonetaryValue);
        var maxPossible = compatibleOffers.Sum(o => o.NormalisedOffer.MaximumPossibleValue);

        stopwatch.Stop();

        var diagnostics = new PurchasePlanDiagnostics(
            stopwatch.ElapsedMilliseconds,
            StrategyName,
            evidences.Count,
            new List<string>()
        );

        var plan = new PurchasePlan(
            path,
            compatibleOffers,
            totalGuaranteed,
            totalExpected,
            maxPossible,
            95.0,
            10.0, // Low complexity effort
            compatibleOffers.Select(o => $"Claim {o.NormalisedOffer.CalculatedOffer.Offer.Title}").ToList(),
            compatibleOffers.Select(o => o.NormalisedOffer.CalculatedOffer.Provider.Name).Distinct().ToList(),
            evidences,
            "Optimized to minimize user effort and actions required during checkout.",
            diagnostics
        );

        return new List<PurchasePlan> { plan };
    }
}

public class BalancedPlanningStrategy(IEnumerable<ICompatibilityEvaluator> evaluators) 
    : BasePlanningStrategy(evaluators), IPurchasePlanningStrategy
{
    public string StrategyName => "Balanced Planning Strategy";

    public bool CanPlan(DiscoveryContext context)
    {
        return true; // Fallback default strategy
    }

    public async Task<IEnumerable<PurchasePlan>> PlanPurchasesAsync(IEnumerable<RankedOffer> rankedOffers, DiscoveryContext context, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var offersList = rankedOffers.ToList();
        
        var compatibleOffers = new List<RankedOffer>();
        var evidences = new List<CompatibilityEvidence>();

        foreach (var offer in offersList)
        {
            var isCompatible = true;
            foreach (var existing in compatibleOffers)
            {
                foreach (var evaluator in Evaluators)
                {
                    var evidence = await evaluator.EvaluateCompatibilityAsync(existing, offer, context, cancellationToken);
                    evidences.Add(evidence);

                    if (evidence.Result == CompatibilityResult.Incompatible)
                    {
                        isCompatible = false;
                        break;
                    }
                }
                if (!isCompatible) break;
            }

            if (isCompatible)
            {
                compatibleOffers.Add(offer);
            }
        }

        var path = BuildPurchasePath(compatibleOffers);
        var totalGuaranteed = compatibleOffers.Sum(o => o.NormalisedOffer.GuaranteedMonetaryValue);
        var totalExpected = compatibleOffers.Sum(o => o.NormalisedOffer.ExpectedMonetaryValue);
        var maxPossible = compatibleOffers.Sum(o => o.NormalisedOffer.MaximumPossibleValue);

        stopwatch.Stop();

        var diagnostics = new PurchasePlanDiagnostics(
            stopwatch.ElapsedMilliseconds,
            StrategyName,
            evidences.Count,
            new List<string>()
        );

        var plan = new PurchasePlan(
            path,
            compatibleOffers,
            totalGuaranteed,
            totalExpected,
            maxPossible,
            90.0,
            15.0, // Moderate effort
            compatibleOffers.Select(o => $"Claim {o.NormalisedOffer.CalculatedOffer.Offer.Title}").ToList(),
            compatibleOffers.Select(o => o.NormalisedOffer.CalculatedOffer.Provider.Name).Distinct().ToList(),
            evidences,
            "Balanced combination of high savings rewards and ease of claim execution.",
            diagnostics
        );

        return new List<PurchasePlan> { plan };
    }
}
