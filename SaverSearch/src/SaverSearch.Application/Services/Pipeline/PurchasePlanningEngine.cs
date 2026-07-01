using SaverSearch.Application.Common.Interfaces;
using SaverSearch.Application.Common.Models.Pipeline;
using SaverSearch.Application.Common.Models.Pipeline.Planning;
using SaverSearch.Application.Common.Models.Pipeline.Ranking;

namespace SaverSearch.Application.Services.Pipeline;

public class PurchasePlanningEngine(IEnumerable<IPurchasePlanningStrategy> strategies) : IPurchasePlanningEngine
{
    private readonly List<IPurchasePlanningStrategy> _strategies = strategies.ToList();

    public async Task<IEnumerable<PurchasePlan>> PlanPurchasesAsync(
        IEnumerable<RankedOffer> rankedOffers,
        DiscoveryContext context,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Enumerable.Empty<PurchasePlan>();
        }

        // 1. Resolve planning strategy
        var strategy = _strategies.FirstOrDefault(s => s.CanPlan(context))
                       ?? _strategies.FirstOrDefault(s => s.StrategyName.Contains("Balanced"));

        if (strategy != null)
        {
            return await strategy.PlanPurchasesAsync(rankedOffers, context, cancellationToken);
        }

        return Enumerable.Empty<PurchasePlan>();
    }
}
