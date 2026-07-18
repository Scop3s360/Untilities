using SaverSearch.Application.Common.Models.Pipeline;
using SaverSearch.Application.Common.Models.Pipeline.Planning;
using SaverSearch.Application.Common.Models.Pipeline.Ranking;

namespace SaverSearch.Application.Common.Interfaces;

public interface ICompatibilityEvaluator
{
    string EvaluatorName { get; }
    Task<CompatibilityEvidence> EvaluateCompatibilityAsync(
        RankedOffer first, 
        RankedOffer second, 
        DiscoveryContext context, 
        CancellationToken cancellationToken = default);
}
