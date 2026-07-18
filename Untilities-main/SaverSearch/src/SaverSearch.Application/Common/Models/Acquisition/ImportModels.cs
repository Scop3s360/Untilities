using SaverSearch.Application.Common.Models.Pipeline;

namespace SaverSearch.Application.Common.Models.Acquisition;

/// <summary>Severity level for a validation warning.</summary>
public enum WarningSeverity { Info, Warning, Error }

/// <summary>A single validation finding for one field of a raw offer.</summary>
public record ValidationWarning(
    string ExternalId,
    string Field,
    string Message,
    WarningSeverity Severity
);

/// <summary>
/// The result of validating a single <see cref="RawProviderOffer"/>.
/// Never throws — collects all findings.
/// </summary>
public record ValidationResult(
    bool IsValid,
    IReadOnlyList<ValidationWarning> Warnings,
    IReadOnlyList<string> Errors
);

/// <summary>
/// Summary result returned by the acquisition engine after running one connector.
/// </summary>
public record ImportJobResult(
    Guid JobId,
    string ProviderName,
    string ConnectorVersion,
    bool Success,
    DateTime StartedAt,
    DateTime CompletedAt,
    long DurationMs,
    int OffersDownloaded,
    int OffersValidated,
    int OffersAdded,
    int OffersUpdated,
    int OffersDeactivated,
    int ValidationWarningCount,
    string? ErrorMessage,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<StageTiming> StageTimings
);

/// <summary>
/// Counts returned by the upsert service for a single batch operation.
/// </summary>
public record UpsertSummary(
    int Inserted,
    int Updated,
    int Deactivated
);
