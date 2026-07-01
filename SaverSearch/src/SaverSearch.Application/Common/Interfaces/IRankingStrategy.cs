using SaverSearch.Application.Common.Models.Pipeline;
using SaverSearch.Application.Common.Models.Pipeline.Normalisation;
using SaverSearch.Application.Common.Models.Pipeline.Ranking;

namespace SaverSearch.Application.Common.Interfaces;

public interface IRankingStrategy
{
    string StrategyName { get; }
    bool CanRank(DiscoveryContext context);
    Task<IEnumerable<RankedOffer>> RankOffersAsync(
        IEnumerable<NormalisedOffer> offers,
        DiscoveryContext context,
        CancellationToken cancellationToken = default);
}
