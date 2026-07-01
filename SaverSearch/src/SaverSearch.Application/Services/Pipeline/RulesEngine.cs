using System.Diagnostics;
using SaverSearch.Application.Common.Interfaces;
using SaverSearch.Application.Common.Models.Pipeline;
using SaverSearch.Application.Common.Models.Pipeline.Rules;

namespace SaverSearch.Application.Services.Pipeline;

public class RulesEngine(IEnumerable<IRuleEvaluator> evaluators) : IRulesEngine
{
    private readonly List<IRuleEvaluator> _evaluators = evaluators.ToList();

    public async Task<RuleEvaluationResult> EvaluateAsync(ResolvedOffer offer, DiscoveryContext context, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        // 1. Trigger all rule evaluations concurrently
        var tasks = _evaluators.Select(async evaluator =>
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return new RuleResult(
                    evaluator.RuleName,
                    evaluator.Category,
                    false,
                    "Critical",
                    "Evaluation cancelled.",
                    "The operation was aborted.",
                    new Dictionary<string, string>(),
                    0
                );
            }
            try
            {
                return await evaluator.EvaluateAsync(offer, context, cancellationToken);
            }
            catch (Exception ex)
            {
                return new RuleResult(
                    evaluator.RuleName,
                    evaluator.Category,
                    false,
                    "Critical",
                    ex.Message,
                    "An internal exception occurred during rule execution.",
                    new Dictionary<string, string>(),
                    0
                );
            }
        });

        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // 2. Aggregate results
        var passedRules = results.Where(r => r.Passed).ToList();
        var failedRules = results.Where(r => !r.Passed).ToList();

        // Eligibility: Eligible if no "Critical" rules failed
        var isEligible = !failedRules.Any(r => r.Severity.Equals("Critical", StringComparison.OrdinalIgnoreCase));

        // Confidence: Fraction of passed rules (defaults to 1.0 if no rules are registered)
        double confidence = results.Length > 0 ? (double)passedRules.Count / results.Length : 1.0;

        // Warnings: Collect reasons for failed non-critical rules (e.g. Warning/Info severity)
        var warnings = failedRules
            .Where(r => r.Severity.Equals("Warning", StringComparison.OrdinalIgnoreCase))
            .Select(r => $"{r.RuleName}: {r.FailureReason}")
            .ToList();

        return new RuleEvaluationResult(
            isEligible,
            confidence * 100.0,
            stopwatch.ElapsedMilliseconds,
            results,
            passedRules,
            failedRules,
            warnings
        );
    }
}
