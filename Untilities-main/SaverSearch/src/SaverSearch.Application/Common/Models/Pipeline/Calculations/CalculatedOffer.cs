using SaverSearch.Application.Dtos;

namespace SaverSearch.Application.Common.Models.Pipeline.Calculations;

public record CalculationStep(string Description, decimal Value);

public record CalculatedOfferDiagnostics(
    long CalculationTimeMs,
    string StrategySelected,
    List<string> Warnings,
    string EngineVersion = "1.0.0"
);

public record CalculatedOffer(
    OfferDto Offer,
    RetailerDto Retailer,
    ProviderDto Provider,
    OfferTypeDto OfferType,
    decimal TargetSpend,
    decimal RawSaving,
    decimal FinalSaving,
    decimal EffectiveSavingPercentage,
    decimal? MaximumSaving,
    bool CapApplied,
    string CalculationStrategy,
    List<CalculationStep> CalculationSteps,
    double ConfidenceScore,
    CalculatedOfferDiagnostics Diagnostics
);
