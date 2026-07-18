using System.Diagnostics;
using Microsoft.Extensions.Options;
using SaverSearch.Application.Common.Interfaces;
using SaverSearch.Application.Common.Models.Pipeline;
using SaverSearch.Application.Common.Models.Pipeline.Normalisation;
using SaverSearch.Application.Common.Models.Pipeline.Ranking;

namespace SaverSearch.Application.Services.Pipeline.Ranking;

public class BestMonetaryValueStrategy : IRankingStrategy
{
    public string StrategyName => "Best Monetary Value Strategy";

    public bool CanRank(DiscoveryContext context)
    {
        return context.Preferences.TryGetValue("RankingStrategy", out var strategy) && 
               strategy.Equals("Monetary", StringComparison.OrdinalIgnoreCase);
    }

    public Task<IEnumerable<RankedOffer>> RankOffersAsync(IEnumerable<NormalisedOffer> offers, DiscoveryContext context, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        var rankedList = offers
            .OrderByDescending(o => o.ExpectedMonetaryValue)
            .Select((offer, index) =>
            {
                var factors = new List<ScoringFactorResult>
                {
                    new ScoringFactorResult("Expected Value", (double)offer.ExpectedMonetaryValue, (double)offer.ExpectedMonetaryValue)
                };

                var diagnostics = new RankedOfferDiagnostics(
                    stopwatch.ElapsedMilliseconds,
                    StrategyName,
                    factors,
                    new List<string>()
                );

                return new RankedOffer(
                    offer,
                    (double)offer.ExpectedMonetaryValue,
                    factors,
                    index + 1,
                    StrategyName,
                    "Highest Expected Monetary Value",
                    diagnostics
                );
            })
            .ToList();

        return Task.FromResult<IEnumerable<RankedOffer>>(rankedList);
    }
}

public class HighestConfidenceStrategy : IRankingStrategy
{
    public string StrategyName => "Highest Confidence Strategy";

    public bool CanRank(DiscoveryContext context)
    {
        return context.Preferences.TryGetValue("RankingStrategy", out var strategy) && 
               strategy.Equals("Confidence", StringComparison.OrdinalIgnoreCase);
    }

    public Task<IEnumerable<RankedOffer>> RankOffersAsync(IEnumerable<NormalisedOffer> offers, DiscoveryContext context, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        var rankedList = offers
            .OrderByDescending(o => o.ConfidenceScore)
            .Select((offer, index) =>
            {
                var factors = new List<ScoringFactorResult>
                {
                    new ScoringFactorResult("Confidence Score", offer.ConfidenceScore, offer.ConfidenceScore)
                };

                var diagnostics = new RankedOfferDiagnostics(
                    stopwatch.ElapsedMilliseconds,
                    StrategyName,
                    factors,
                    new List<string>()
                );

                return new RankedOffer(
                    offer,
                    offer.ConfidenceScore,
                    factors,
                    index + 1,
                    StrategyName,
                    "Highest Confidence Score",
                    diagnostics
                );
            })
            .ToList();

        return Task.FromResult<IEnumerable<RankedOffer>>(rankedList);
    }
}

public class LowestComplexityStrategy : IRankingStrategy
{
    public string StrategyName => "Lowest Complexity Strategy";

    public bool CanRank(DiscoveryContext context)
    {
        return context.Preferences.TryGetValue("RankingStrategy", out var strategy) && 
               strategy.Equals("Complexity", StringComparison.OrdinalIgnoreCase);
    }

    public Task<IEnumerable<RankedOffer>> RankOffersAsync(IEnumerable<NormalisedOffer> offers, DiscoveryContext context, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var factor = new ComplexityScoreFactor();

        var rankedList = offers
            .Select(o => new { Offer = o, SimplicityScore = factor.CalculateScore(o, context) })
            .OrderByDescending(x => x.SimplicityScore)
            .Select((x, index) =>
            {
                var factors = new List<ScoringFactorResult>
                {
                    new ScoringFactorResult("Simplicity Score", x.SimplicityScore, x.SimplicityScore)
                };

                var diagnostics = new RankedOfferDiagnostics(
                    stopwatch.ElapsedMilliseconds,
                    StrategyName,
                    factors,
                    new List<string>()
                );

                return new RankedOffer(
                    x.Offer,
                    x.SimplicityScore,
                    factors,
                    index + 1,
                    StrategyName,
                    "Lowest Redemption Complexity",
                    diagnostics
                );
            })
            .ToList();

        return Task.FromResult<IEnumerable<RankedOffer>>(rankedList);
    }
}

public class BalancedRankingStrategy(
    IOptions<RankingSettings> settings,
    IEnumerable<IScoringFactor> scoringFactors) : IRankingStrategy
{
    private readonly RankingSettings _settings = settings.Value;
    private readonly List<IScoringFactor> _scoringFactors = scoringFactors.ToList();

    public string StrategyName => "Balanced Ranking Strategy";

    public bool CanRank(DiscoveryContext context)
    {
        // Default fallback/balanced ranking
        return true;
    }

    public Task<IEnumerable<RankedOffer>> RankOffersAsync(IEnumerable<NormalisedOffer> offers, DiscoveryContext context, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        var rankedList = offers.Select(offer =>
        {
            var factorResults = new List<ScoringFactorResult>();
            double overallScore = 0.0;

            foreach (var factor in _scoringFactors)
            {
                var raw = factor.CalculateScore(offer, context);
                double weight = 1.0;

                if (factor is ExpectedValueFactor) weight = _settings.ExpectedValueWeight;
                else if (factor is GuaranteedValueFactor) weight = _settings.GuaranteedValueWeight;
                else if (factor is ConfidenceScoreFactor) weight = _settings.ConfidenceWeight;
                else if (factor is ComplexityScoreFactor) weight = _settings.ComplexityWeight;

                var weighted = raw * weight;
                overallScore += weighted;

                factorResults.Add(new ScoringFactorResult(factor.FactorName, raw, weighted));
            }

            var diagnostics = new RankedOfferDiagnostics(
                stopwatch.ElapsedMilliseconds,
                StrategyName,
                factorResults,
                new List<string>()
            );

            return new RankedOffer(
                offer,
                overallScore,
                factorResults,
                0, // Set after sorting
                StrategyName,
                "Balanced blend of monetary return, reliability and redemption simplicity.",
                diagnostics
            );
        })
        .OrderByDescending(ro => ro.OverallScore)
        .Select((ro, index) => ro with { RankingPosition = index + 1 })
        .ToList();

        stopwatch.Stop();
        return Task.FromResult<IEnumerable<RankedOffer>>(rankedList);
    }
}
