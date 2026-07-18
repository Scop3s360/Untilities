using SaverSearch.Application.Common.Models.Pipeline.Planning;

namespace SaverSearch.Application.Common.Models.Pipeline.Recommendations;

public enum RecommendationType
{
    BestOverall,
    MaximumSaving,
    LowestComplexity,
    HighestConfidence,
    LowestRisk,
    Balanced
}

public enum RiskLevel
{
    Low,
    Medium,
    High
}

public enum FeedbackStatus
{
    Accepted,
    Rejected,
    Ignored
}

public record RiskAnalysis(
    double RiskScore,
    RiskLevel RiskLevel,
    List<string> RiskFactors
);

public record RecommendationReasoning(
    string SelectionJustification,
    List<string> KeyStrengths,
    List<string> PotentialRisks
);

public record AlternativeRecommendation(
    PurchasePlan PurchasePlan,
    string RejectionReason
);

public record RecommendationDiagnostics(
    long RecommendationDurationMs,
    string StrategySelected,
    double DecisionScore,
    List<string> Warnings,
    string EngineVersion = "1.0.0"
);

public record DecisionPackage(
    string Title,
    RecommendationType RecommendationType,
    PurchasePlan PurchasePlan,
    decimal EstimatedSaving,
    decimal GuaranteedSaving,
    decimal MaximumSaving,
    double Confidence,
    RiskLevel RiskLevel,
    double UserEffort,
    RecommendationReasoning Reasoning,
    List<AlternativeRecommendation> Alternatives,
    List<string> Warnings,
    RecommendationDiagnostics Diagnostics
);
