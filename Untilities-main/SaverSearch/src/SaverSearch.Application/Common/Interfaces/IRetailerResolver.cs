using SaverSearch.Application.Common.Models.Pipeline;
using SaverSearch.Application.Common.Models.Resolver;

namespace SaverSearch.Application.Common.Interfaces;

public interface IRetailerResolver
{
    Task<RetailerResolverResponse> ResolveAsync(
        DiscoveryContext context, 
        CancellationToken cancellationToken = default);
}
