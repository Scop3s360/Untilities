using SaverSearch.Application.Common.Models.Pipeline;
using SaverSearch.Application.Common.Models.Pipeline.Planning;
using SaverSearch.Application.Common.Models.Pipeline.Recommendations;

namespace SaverSearch.Application.Common.Interfaces;

public interface IRecommendationEngine
{
    Task<DecisionPackage> RecommendBestPlanAsync(
        IEnumerable<PurchasePlan> plans,
        DiscoveryContext context,
        CancellationToken cancellationToken = default);
}
