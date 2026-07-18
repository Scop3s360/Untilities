using SaverSearch.Application.Common.Models.Pipeline.Ranking;

namespace SaverSearch.Application.Common.Models.Pipeline.Planning;

public enum CompatibilityResult
{
    Compatible,
    Incompatible,
    Unknown
}

public record CompatibilityEvidence(
    CompatibilityResult Result,
    double Confidence,
    string Explanation,
    string Source,
    long EvaluationTimeMs
);

public record PurchasePathStep(int StepNumber, string ActionDescription, string Explanation);

public record PurchasePlanDiagnostics(
    long PlanningDurationMs,
    string StrategySelected,
    int CompatibilityEvaluationsCount,
    List<string> Warnings,
    string EngineVersion = "1.0.0"
);

public record PurchasePlan(
    List<PurchasePathStep> PurchasePath,
    List<RankedOffer> IncludedOffers,
    decimal TotalGuaranteedSaving,
    decimal TotalExpectedSaving,
    decimal MaximumPossibleSaving,
    double OverallConfidence,
    double EstimatedUserEffort,
    List<string> RequiredUserActions,
    List<string> RequiredAccounts,
    List<CompatibilityEvidence> CompatibilityEvidences,
    string Explanation,
    PurchasePlanDiagnostics Diagnostics
);
