using SaverSearch.Application.Common.Interfaces;
using SaverSearch.Application.Common.Models.Pipeline;
using SaverSearch.Application.Common.Models.Pipeline.Planning;
using SaverSearch.Application.Common.Models.Pipeline.Recommendations;

namespace SaverSearch.Application.Services.Pipeline;

public class RecommendationEngine(IEnumerable<IRecommendationStrategy> strategies) : IRecommendationEngine
{
    private readonly List<IRecommendationStrategy> _strategies = strategies.ToList();

    public async Task<DecisionPackage> RecommendBestPlanAsync(
        IEnumerable<PurchasePlan> plans,
        DiscoveryContext context,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested || plans == null || !plans.Any())
        {
            throw new ArgumentException("Plans collection cannot be empty.");
        }

        // 1. Resolve strategy matching user intent
        var strategy = _strategies.FirstOrDefault(s => s.CanRecommend(context))
                       ?? _strategies.FirstOrDefault(s => s.StrategyType == RecommendationType.Balanced);

        if (strategy != null)
        {
            return await strategy.RecommendAsync(plans, context, cancellationToken);
        }

        throw new InvalidOperationException("No suitable recommendation strategy resolved.");
    }
}
