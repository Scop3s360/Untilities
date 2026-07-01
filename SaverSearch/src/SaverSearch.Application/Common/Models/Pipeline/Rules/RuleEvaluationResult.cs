namespace SaverSearch.Application.Common.Models.Pipeline.Rules;

public record RuleEvaluationResult(
    bool IsEligible,
    double ConfidenceScore,
    long EvaluationTimeMs,
    IEnumerable<RuleResult> RulesEvaluated,
    IEnumerable<RuleResult> PassedRules,
    IEnumerable<RuleResult> FailedRules,
    IEnumerable<string> Warnings,
    string EngineVersion = "1.0.0"
);
