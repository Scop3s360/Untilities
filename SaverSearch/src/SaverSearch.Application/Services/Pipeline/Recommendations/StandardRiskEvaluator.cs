using SaverSearch.Application.Common.Interfaces;
using SaverSearch.Application.Common.Models.Pipeline;
using SaverSearch.Application.Common.Models.Pipeline.Planning;
using SaverSearch.Application.Common.Models.Pipeline.Recommendations;

namespace SaverSearch.Application.Services.Pipeline.Recommendations;

public class StandardRiskEvaluator : IRiskEvaluator
{
    public string EvaluatorName => "Standard Stacking Risk Evaluator";

    public RiskAnalysis EvaluateRisk(PurchasePlan plan, DiscoveryContext context)
    {
        double score = 0.0;
        var factors = new List<string>();

        // 1. Check compatibility results
        var unknowns = plan.CompatibilityEvidences.Count(e => e.Result == CompatibilityResult.Unknown);
        if (unknowns > 0)
        {
            score += unknowns * 15.0;
            factors.Add($"{unknowns} offer pairings have unknown compatibility rules.");
        }

        // 2. Check confidence score
        if (plan.OverallConfidence < 90.0)
        {
            score += (100.0 - plan.OverallConfidence) * 0.8;
            factors.Add($"Overall confidence is below 90% (currently {plan.OverallConfidence}%).");
        }

        // 3. Complexity effort
        if (plan.EstimatedUserEffort > 15.0)
        {
            score += (plan.EstimatedUserEffort - 15.0) * 1.5;
            factors.Add($"Checkout complexity is higher than average (currently {plan.EstimatedUserEffort}).");
        }

        // Bound to 0 - 100
        score = Math.Clamp(score, 0.0, 100.0);

        var level = RiskLevel.Low;
        if (score > 60.0) level = RiskLevel.High;
        else if (score > 30.0) level = RiskLevel.Medium;

        return new RiskAnalysis(score, level, factors);
    }
}
