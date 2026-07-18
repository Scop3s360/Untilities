using SaverSearch.Application.Common.Interfaces;
using SaverSearch.Application.Common.Models.Pipeline;
using SaverSearch.Application.Common.Models.Pipeline.Normalisation;
using SaverSearch.Application.Common.Models.Pipeline.Ranking;

namespace SaverSearch.Application.Services.Pipeline;

public class RankingEngine(IEnumerable<IRankingStrategy> strategies) : IRankingEngine
{
    private readonly List<IRankingStrategy> _strategies = strategies.ToList();

    public async Task<IEnumerable<RankedOffer>> RankOffersAsync(
        IEnumerable<NormalisedOffer> offers,
        DiscoveryContext context,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Enumerable.Empty<RankedOffer>();
        }

        // 1. Locate matching ranking strategy (e.g. monetary, confidence, complexity, or balanced)
        var strategy = _strategies.FirstOrDefault(s => s.CanRank(context)) 
                       ?? _strategies.FirstOrDefault(s => s.StrategyName.Contains("Balanced"));

        if (strategy != null)
        {
            return await strategy.RankOffersAsync(offers, context, cancellationToken);
        }

        return Enumerable.Empty<RankedOffer>();
    }
}
