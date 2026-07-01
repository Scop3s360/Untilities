using SaverSearch.Application.Common.Models.Pipeline.Normalisation;

namespace SaverSearch.Application.Common.Models.Pipeline.Ranking;

public record ScoringFactorResult(string FactorName, double RawScore, double WeightedScore);

public record RankedOfferDiagnostics(
    long RankingTimeMs,
    string StrategySelected,
    List<ScoringFactorResult> ScoringBreakdown,
    List<string> Warnings,
    string EngineVersion = "1.0.0"
);

public record RankedOffer(
    NormalisedOffer NormalisedOffer,
    double OverallScore,
    List<ScoringFactorResult> ScoringFactors,
    int RankingPosition,
    string RankingStrategyUsed,
    string Explanation,
    RankedOfferDiagnostics Diagnostics
);

public class RankingSettings
{
    public double ExpectedValueWeight { get; set; } = 0.40;
    public double GuaranteedValueWeight { get; set; } = 0.20;
    public double ConfidenceWeight { get; set; } = 0.20;
    public double ComplexityWeight { get; set; } = 0.20;
    public double DefaultStrategyWeight { get; set; } = 1.0;
}
