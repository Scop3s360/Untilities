using SaverSearch.Application.Common.Models.Pipeline;
using SaverSearch.Application.Common.Models.Pipeline.Planning;
using SaverSearch.Application.Common.Models.Pipeline.Recommendations;

namespace SaverSearch.Application.Common.Interfaces;

public interface IRecommendationStrategy
{
    RecommendationType StrategyType { get; }
    bool CanRecommend(DiscoveryContext context);
    Task<DecisionPackage> RecommendAsync(
        IEnumerable<PurchasePlan> plans,
        DiscoveryContext context,
        CancellationToken cancellationToken = default);
}
