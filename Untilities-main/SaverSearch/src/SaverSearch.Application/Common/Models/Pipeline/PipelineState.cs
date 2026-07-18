using SaverSearch.Application.Common.Models.Pipeline.Calculations;
using SaverSearch.Application.Dtos;

namespace SaverSearch.Application.Common.Models.Pipeline;

public record PipelineDiagnostics
{
    public List<StageTiming> Timings { get; init; } = new();
    public bool IsSuccess { get; init; } = true;
    public string? FailedStage { get; init; }
    public string? ErrorMessage { get; init; }
}

public record StageTiming(string StageName, long ElapsedMilliseconds);

public record OfferStackGroup(
    List<CalculatedOffer> Offers,
    decimal TotalSavingAmount,
    decimal EffectiveSavingRate,
    string CompatibilityRationale
);

public record DiscoveryRecommendation(
    string StrategyName,
    List<OfferStackGroup> RecommendedStacks,
    string UserInstructionHtml
);

public record PipelineState(DiscoveryContext Context)
{
    public RetailerDto? Retailer { get; init; }
    public IEnumerable<OfferDto>? ResolvedOffers { get; init; }
    public IEnumerable<OfferDto>? EligibleOffers { get; init; }
    public IEnumerable<CalculatedOffer>? CalculatedOffers { get; init; }
    public IEnumerable<OfferDto>? RankedOffers { get; init; } // Placeholder
    public IEnumerable<OfferStackGroup>? StackGroups { get; init; }
    public IEnumerable<DiscoveryRecommendation>? Recommendations { get; init; }
    public double ConfidenceScore { get; init; } = 1.0;
    public PipelineDiagnostics Diagnostics { get; init; } = new();
}
