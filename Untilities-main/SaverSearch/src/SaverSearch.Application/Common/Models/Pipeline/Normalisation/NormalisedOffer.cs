using SaverSearch.Application.Common.Models.Pipeline.Calculations;

namespace SaverSearch.Application.Common.Models.Pipeline.Normalisation;

public record NormalisationStep(string Description, decimal Value);

public record NormalisationDiagnostics(
    long ProcessingTimeMs,
    string StrategySelected,
    string ConversionSource,
    List<string> Warnings,
    string EngineVersion = "1.0.0"
);

public record NormalisedOffer(
    CalculatedOffer CalculatedOffer,
    decimal ExpectedMonetaryValue,
    decimal GuaranteedMonetaryValue,
    decimal MaximumPossibleValue,
    decimal EffectiveSavingPercentage,
    double ConfidenceScore,
    string ConversionMethod,
    string NormalisationStrategy,
    List<NormalisationStep> AuditTrail,
    NormalisationDiagnostics Diagnostics
);

public class NormalisationSettings
{
    public double CashbackConfidence { get; set; } = 100.0;
    public double DiscountConfidence { get; set; } = 100.0;
    public double PointsConfidence { get; set; } = 85.0;
    public double MortgageConfidence { get; set; } = 100.0;
    public double DefaultConfidence { get; set; } = 80.0;
}
