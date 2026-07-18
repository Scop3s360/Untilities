namespace SaverSearch.Application.Dtos.Discovery;

/// <summary>A single step in the recommended purchase path.</summary>
public record PurchasePathStepDto(
    int StepNumber,
    string ActionDescription,
    string Explanation
);

/// <summary>Summary of a single included offer within a plan.</summary>
public record IncludedOfferSummaryDto(
    Guid OfferId,
    string OfferTitle,
    string ProviderName,
    string RetailerName,
    decimal ExpectedSaving
);

/// <summary>An alternative purchase plan that was not selected as the primary recommendation.</summary>
public record AlternativePlanDto(
    decimal EstimatedSaving,
    double Confidence,
    string RejectionReason
);

/// <summary>Diagnostics describing how the pipeline performed during this request.</summary>
public record SearchDiagnosticsDto(
    long TotalExecutionMs,
    string StrategySelected,
    string RecommendationStrategy,
    List<StageTimingDto> StageTimings,
    List<string> Warnings,
    string EngineVersion = "1.0.0"
);

/// <summary>Duration of an individual pipeline stage.</summary>
public record StageTimingDto(string StageName, long ElapsedMs);

/// <summary>
/// Response body for POST /api/discover.
/// </summary>
public class DiscoveryResponse
{
    /// <summary>Whether the discovery pipeline completed successfully.</summary>
    public bool Success { get; set; }

    /// <summary>Title of the primary recommendation.</summary>
    public string? RecommendationTitle { get; set; }

    /// <summary>The primary recommended purchase plan.</summary>
    public PrimaryPlanDto? RecommendedPlan { get; set; }

    /// <summary>Alternative plans that were not selected.</summary>
    public List<AlternativePlanDto> AlternativePlans { get; set; } = [];

    /// <summary>Search and pipeline diagnostics.</summary>
    public SearchDiagnosticsDto? Diagnostics { get; set; }

    /// <summary>Total wall-clock execution time in milliseconds.</summary>
    public long ExecutionTimeMs { get; set; }

    /// <summary>Non-fatal warnings generated during discovery.</summary>
    public List<string> Warnings { get; set; } = [];

    /// <summary>Errors, populated only when Success is false.</summary>
    public List<string> Errors { get; set; } = [];

    /// <summary>Correlation ID for request tracing.</summary>
    public string? CorrelationId { get; set; }
}

/// <summary>The primary recommended plan returned to the caller.</summary>
public class PrimaryPlanDto
{
    /// <summary>Expected total monetary saving.</summary>
    public decimal EstimatedSaving { get; set; }

    /// <summary>Guaranteed (floor) monetary saving.</summary>
    public decimal GuaranteedSaving { get; set; }

    /// <summary>Maximum possible monetary saving.</summary>
    public decimal MaximumSaving { get; set; }

    /// <summary>Overall payout confidence percentage (0–100).</summary>
    public double Confidence { get; set; }

    /// <summary>Risk level: Low, Medium, or High.</summary>
    public string RiskLevel { get; set; } = "Low";

    /// <summary>Estimated user effort score.</summary>
    public double UserEffort { get; set; }

    /// <summary>Human-readable justification for this recommendation.</summary>
    public string? SelectionJustification { get; set; }

    /// <summary>Key strengths of this plan.</summary>
    public List<string> KeyStrengths { get; set; } = [];

    /// <summary>Potential risks to be aware of.</summary>
    public List<string> PotentialRisks { get; set; } = [];

    /// <summary>Sequential purchase path steps.</summary>
    public List<PurchasePathStepDto> PurchasePath { get; set; } = [];

    /// <summary>Summary of included offers.</summary>
    public List<IncludedOfferSummaryDto> IncludedOffers { get; set; } = [];

    /// <summary>Actions the user must complete to claim savings.</summary>
    public List<string> RequiredUserActions { get; set; } = [];

    /// <summary>Accounts required to claim all offers in this plan.</summary>
    public List<string> RequiredAccounts { get; set; } = [];
}
