using SaverSearch.Domain.Entities;

namespace SaverSearch.Application.Common.Models.Resolver;

public record ResolverResult(
    Retailer Retailer,
    double ConfidenceScore,
    string MatchType,
    string Reason
);

public record StrategyTiming(string StrategyName, long ElapsedMilliseconds);

public record ResolverDiagnostics(
    List<StrategyTiming> ExecutedStrategies,
    string WinningStrategy,
    string Rationale
);

public record RetailerResolverResponse(
    Retailer? MatchedRetailer,
    double ConfidenceScore,
    string MatchType,
    IEnumerable<ResolverResult> AlternativeMatches,
    ResolverDiagnostics Diagnostics
);
