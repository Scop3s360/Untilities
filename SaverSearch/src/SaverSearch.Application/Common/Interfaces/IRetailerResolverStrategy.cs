using SaverSearch.Application.Common.Models.Pipeline;
using SaverSearch.Application.Common.Models.Resolver;
using SaverSearch.Domain.Entities;

namespace SaverSearch.Application.Common.Interfaces;

public interface IRetailerResolverStrategy
{
    string StrategyName { get; }
    Task<IEnumerable<ResolverResult>> MatchAsync(
        DiscoveryContext context, 
        IEnumerable<Retailer> retailers, 
        CancellationToken cancellationToken = default);
}
