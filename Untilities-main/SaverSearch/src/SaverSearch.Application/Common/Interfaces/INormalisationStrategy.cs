using SaverSearch.Application.Common.Models.Pipeline;
using SaverSearch.Application.Common.Models.Pipeline.Calculations;
using SaverSearch.Application.Common.Models.Pipeline.Normalisation;

namespace SaverSearch.Application.Common.Interfaces;

public interface INormalisationStrategy
{
    string StrategyName { get; }
    bool CanNormalise(CalculatedOffer calculatedOffer);
    Task<NormalisedOffer> NormaliseAsync(
        CalculatedOffer calculatedOffer,
        DiscoveryContext context,
        CancellationToken cancellationToken = default);
}
