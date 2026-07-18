using SaverSearch.Application.Common.Models.Pipeline;
using SaverSearch.Application.Common.Models.Pipeline.Normalisation;
using SaverSearch.Application.Common.Models.Pipeline.Ranking;

namespace SaverSearch.Application.Common.Interfaces;

public interface IRankingEngine
{
    Task<IEnumerable<RankedOffer>> RankOffersAsync(
        IEnumerable<NormalisedOffer> offers,
        DiscoveryContext context,
        CancellationToken cancellationToken = default);
}
// This interface allows the engine to consume NormalisedOffer lists.
