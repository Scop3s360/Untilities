using SaverSearch.Application.Common.Models.Pipeline;
using SaverSearch.Application.Dtos;

namespace SaverSearch.Application.Common.Interfaces;

public interface IOfferResolver
{
    Task<OfferResolverResponse> ResolveOffersAsync(
        DiscoveryContext context,
        RetailerDto retailer,
        CancellationToken cancellationToken = default);
}
// Interface prepared for future extension (e.g. multi-source fetching, Redis Caching, External API integrations)
