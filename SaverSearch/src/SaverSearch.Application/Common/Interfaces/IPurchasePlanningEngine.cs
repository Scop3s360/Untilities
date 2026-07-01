using SaverSearch.Application.Common.Models.Pipeline;
using SaverSearch.Application.Common.Models.Pipeline.Planning;
using SaverSearch.Application.Common.Models.Pipeline.Ranking;

namespace SaverSearch.Application.Common.Interfaces;

public interface IPurchasePlanningEngine
{
    Task<IEnumerable<PurchasePlan>> PlanPurchasesAsync(
        IEnumerable<RankedOffer> rankedOffers,
        DiscoveryContext context,
        CancellationToken cancellationToken = default);
}
// Interface prepared for the next recommendation engine phases.
