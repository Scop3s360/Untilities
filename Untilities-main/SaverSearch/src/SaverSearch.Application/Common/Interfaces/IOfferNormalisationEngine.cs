using SaverSearch.Application.Common.Models.Pipeline;
using SaverSearch.Application.Common.Models.Pipeline.Calculations;
using SaverSearch.Application.Common.Models.Pipeline.Normalisation;

namespace SaverSearch.Application.Common.Interfaces;

public interface IOfferNormalisationEngine
{
    Task<IEnumerable<NormalisedOffer>> NormaliseOffersAsync(
        IEnumerable<CalculatedOffer> calculatedOffers,
        DiscoveryContext context,
        CancellationToken cancellationToken = default);
}
