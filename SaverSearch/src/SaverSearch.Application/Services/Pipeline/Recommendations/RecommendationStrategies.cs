using System.Diagnostics;
using SaverSearch.Application.Common.Interfaces;
using SaverSearch.Application.Common.Models.Pipeline;
using SaverSearch.Application.Common.Models.Pipeline.Planning;
using SaverSearch.Application.Common.Models.Pipeline.Recommendations;

namespace SaverSearch.Application.Services.Pipeline.Recommendations;

public abstract class BaseRecommendationStrategy(IEnumerable<IRiskEvaluator> riskEvaluators)
{
    protected readonly List<IRiskEvaluator> RiskEvaluators = riskEvaluators.ToList();

    protected RiskAnalysis AnalyzePlanRisk(PurchasePlan plan, DiscoveryContext context)
    {
        var evaluator = RiskEvaluators.FirstOrDefault() ?? new StandardRiskEvaluator();
        return evaluator.EvaluateRisk(plan, context);
    }

    protected List<AlternativeRecommendation> BuildAlternatives(List<PurchasePlan> allPlans, PurchasePlan selectedPlan)
    {
        return allPlans
            .Where(p => p != selectedPlan)
            .Select(p =>
            {
                string reason = "Lower overall expected savings compared to the main plan.";
                if (p.TotalExpectedSaving > selectedPlan.TotalExpectedSaving && p.EstimatedUserEffort > selectedPlan.EstimatedUserEffort)
                {
                    reason = "Higher expected savings but requires significantly more user checkout actions.";
                }
                else if (p.OverallConfidence < selectedPlan.OverallConfidence)
                {
                    reason = "Lower payout confidence metrics.";
                }

                return new AlternativeRecommendation(p, reason);
            })
            .ToList();
    }
}

public class BestOverallRecommendationStrategy(IEnumerable<IRiskEvaluator> riskEvaluators)
    : BaseRecommendationStrategy(riskEvaluators), IRecommendationStrategy
{
    public RecommendationType StrategyType => RecommendationType.BestOverall;

    public bool CanRecommend(DiscoveryContext context)
    {
        return context.Preferences.TryGetValue("UserIntent", out var intent) &&
               intent.Equals("BestOverall", StringComparison.OrdinalIgnoreCase);
    }

    public Task<DecisionPackage> RecommendAsync(IEnumerable<PurchasePlan> plans, DiscoveryContext context, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var allPlans = plans.ToList();
        
        // Sort by expected saving descending, then lowest effort
        var selected = allPlans
            .OrderByDescending(p => p.TotalExpectedSaving)
            .ThenBy(p => p.EstimatedUserEffort)
            .FirstOrDefault() ?? allPlans.First();

        var risk = AnalyzePlanRisk(selected, context);
        var alts = BuildAlternatives(allPlans, selected);
        stopwatch.Stop();

        var diag = new RecommendationDiagnostics(stopwatch.ElapsedMilliseconds, nameof(BestOverallRecommendationStrategy), 98.0, new List<string>());

        var reasoning = new RecommendationReasoning(
            "Highest saving rate with optimal ease of checkout.",
            new List<string> { "Combines complementary voucher codes and portal cashback." },
            risk.RiskFactors
        );

        return Task.FromResult(new DecisionPackage(
            "Best Overall Savings Plan",
            StrategyType,
            selected,
            selected.TotalExpectedSaving,
            selected.TotalGuaranteedSaving,
            selected.MaximumPossibleSaving,
            selected.OverallConfidence,
            risk.RiskLevel,
            selected.EstimatedUserEffort,
            reasoning,
            alts,
            new List<string>(),
            diag
        ));
    }
}

public class MaximumSavingRecommendationStrategy(IEnumerable<IRiskEvaluator> riskEvaluators)
    : BaseRecommendationStrategy(riskEvaluators), IRecommendationStrategy
{
    public RecommendationType StrategyType => RecommendationType.MaximumSaving;

    public bool CanRecommend(DiscoveryContext context)
    {
        return context.Preferences.TryGetValue("UserIntent", out var intent) &&
               intent.Equals("MaximumSavings", StringComparison.OrdinalIgnoreCase);
    }

    public Task<DecisionPackage> RecommendAsync(IEnumerable<PurchasePlan> plans, DiscoveryContext context, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var allPlans = plans.ToList();
        
        var selected = allPlans.OrderByDescending(p => p.TotalExpectedSaving).FirstOrDefault() ?? allPlans.First();

        var risk = AnalyzePlanRisk(selected, context);
        var alts = BuildAlternatives(allPlans, selected);
        stopwatch.Stop();

        var diag = new RecommendationDiagnostics(stopwatch.ElapsedMilliseconds, nameof(MaximumSavingRecommendationStrategy), 100.0, new List<string>());

        var reasoning = new RecommendationReasoning(
            "Delivers the absolute maximum saving return available.",
            new List<string> { "Includes all stackable gift cards and portal links." },
            risk.RiskFactors
        );

        return Task.FromResult(new DecisionPackage(
            "Max Savings Plan",
            StrategyType,
            selected,
            selected.TotalExpectedSaving,
            selected.TotalGuaranteedSaving,
            selected.MaximumPossibleSaving,
            selected.OverallConfidence,
            risk.RiskLevel,
            selected.EstimatedUserEffort,
            reasoning,
            alts,
            new List<string>(),
            diag
        ));
    }
}

public class LowestComplexityRecommendationStrategy(IEnumerable<IRiskEvaluator> riskEvaluators)
    : BaseRecommendationStrategy(riskEvaluators), IRecommendationStrategy
{
    public RecommendationType StrategyType => RecommendationType.LowestComplexity;

    public bool CanRecommend(DiscoveryContext context)
    {
        return context.Preferences.TryGetValue("UserIntent", out var intent) &&
               intent.Equals("FastestCheckout", StringComparison.OrdinalIgnoreCase);
    }

    public Task<DecisionPackage> RecommendAsync(IEnumerable<PurchasePlan> plans, DiscoveryContext context, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var allPlans = plans.ToList();
        
        var selected = allPlans.OrderBy(p => p.EstimatedUserEffort).FirstOrDefault() ?? allPlans.First();

        var risk = AnalyzePlanRisk(selected, context);
        var alts = BuildAlternatives(allPlans, selected);
        stopwatch.Stop();

        var diag = new RecommendationDiagnostics(stopwatch.ElapsedMilliseconds, nameof(LowestComplexityRecommendationStrategy), 95.0, new List<string>());

        var reasoning = new RecommendationReasoning(
            "Requires the lowest manual effort to claim.",
            new List<string> { "Avoids complex multi-step voucher purchases." },
            risk.RiskFactors
        );

        return Task.FromResult(new DecisionPackage(
            "Simple Redemption Plan",
            StrategyType,
            selected,
            selected.TotalExpectedSaving,
            selected.TotalGuaranteedSaving,
            selected.MaximumPossibleSaving,
            selected.OverallConfidence,
            risk.RiskLevel,
            selected.EstimatedUserEffort,
            reasoning,
            alts,
            new List<string>(),
            diag
        ));
    }
}

public class HighestConfidenceRecommendationStrategy(IEnumerable<IRiskEvaluator> riskEvaluators)
    : BaseRecommendationStrategy(riskEvaluators), IRecommendationStrategy
{
    public RecommendationType StrategyType => RecommendationType.HighestConfidence;

    public bool CanRecommend(DiscoveryContext context)
    {
        return context.Preferences.TryGetValue("UserIntent", out var intent) &&
               intent.Equals("HighestConfidence", StringComparison.OrdinalIgnoreCase);
    }

    public Task<DecisionPackage> RecommendAsync(IEnumerable<PurchasePlan> plans, DiscoveryContext context, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var allPlans = plans.ToList();
        
        var selected = allPlans.OrderByDescending(p => p.OverallConfidence).FirstOrDefault() ?? allPlans.First();

        var risk = AnalyzePlanRisk(selected, context);
        var alts = BuildAlternatives(allPlans, selected);
        stopwatch.Stop();

        var diag = new RecommendationDiagnostics(stopwatch.ElapsedMilliseconds, nameof(HighestConfidenceRecommendationStrategy), 96.0, new List<string>());

        var reasoning = new RecommendationReasoning(
            "Prioritizes offers with the highest probability of payout verification.",
            new List<string> { "Filters out rewards from unverified or low-confidence sources." },
            risk.RiskFactors
        );

        return Task.FromResult(new DecisionPackage(
            "High Payout Guarantee Plan",
            StrategyType,
            selected,
            selected.TotalExpectedSaving,
            selected.TotalGuaranteedSaving,
            selected.MaximumPossibleSaving,
            selected.OverallConfidence,
            risk.RiskLevel,
            selected.EstimatedUserEffort,
            reasoning,
            alts,
            new List<string>(),
            diag
        ));
    }
}

public class LowestRiskRecommendationStrategy(IEnumerable<IRiskEvaluator> riskEvaluators)
    : BaseRecommendationStrategy(riskEvaluators), IRecommendationStrategy
{
    public RecommendationType StrategyType => RecommendationType.LowestRisk;

    public bool CanRecommend(DiscoveryContext context)
    {
        return context.Preferences.TryGetValue("UserIntent", out var intent) &&
               intent.Equals("LowestRisk", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<DecisionPackage> RecommendAsync(IEnumerable<PurchasePlan> plans, DiscoveryContext context, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var allPlans = plans.ToList();
        
        // Find plan with lowest risk score
        PurchasePlan selected = allPlans.First();
        double bestRisk = double.MaxValue;

        foreach (var p in allPlans)
        {
            var riskScore = AnalyzePlanRisk(p, context).RiskScore;
            if (riskScore < bestRisk)
            {
                bestRisk = riskScore;
                selected = p;
            }
        }

        var risk = AnalyzePlanRisk(selected, context);
        var alts = BuildAlternatives(allPlans, selected);
        stopwatch.Stop();

        var diag = new RecommendationDiagnostics(stopwatch.ElapsedMilliseconds, nameof(LowestRiskRecommendationStrategy), 94.0, new List<string>());

        var reasoning = new RecommendationReasoning(
            "Recommended to minimize purchase failures and tracking issues.",
            new List<string> { "Excludes unverified stacking combinations." },
            risk.RiskFactors
        );

        return new DecisionPackage(
            "Safe Purchase Plan",
            StrategyType,
            selected,
            selected.TotalExpectedSaving,
            selected.TotalGuaranteedSaving,
            selected.MaximumPossibleSaving,
            selected.OverallConfidence,
            risk.RiskLevel,
            selected.EstimatedUserEffort,
            reasoning,
            alts,
            new List<string>(),
            diag
        );
    }
}

public class BalancedRecommendationStrategy(IEnumerable<IRiskEvaluator> riskEvaluators)
    : BaseRecommendationStrategy(riskEvaluators), IRecommendationStrategy
{
    public RecommendationType StrategyType => RecommendationType.Balanced;

    public bool CanRecommend(DiscoveryContext context)
    {
        return true; // Fallback default strategy
    }

    public Task<DecisionPackage> RecommendAsync(IEnumerable<PurchasePlan> plans, DiscoveryContext context, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var allPlans = plans.ToList();
        
        var selected = allPlans.FirstOrDefault() ?? allPlans.First();

        var risk = AnalyzePlanRisk(selected, context);
        var alts = BuildAlternatives(allPlans, selected);
        stopwatch.Stop();

        var diag = new RecommendationDiagnostics(stopwatch.ElapsedMilliseconds, nameof(BalancedRecommendationStrategy), 95.0, new List<string>());

        var reasoning = new RecommendationReasoning(
            "Balanced choice between savings return and payout safety.",
            new List<string> { "Standard recommended stacking package." },
            risk.RiskFactors
        );

        return Task.FromResult(new DecisionPackage(
            "Balanced Savings Plan",
            StrategyType,
            selected,
            selected.TotalExpectedSaving,
            selected.TotalGuaranteedSaving,
            selected.MaximumPossibleSaving,
            selected.OverallConfidence,
            risk.RiskLevel,
            selected.EstimatedUserEffort,
            reasoning,
            alts,
            new List<string>(),
            diag
        ));
    }
}
