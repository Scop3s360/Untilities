using SaverSearch.Application.Common.Models.Pipeline;
using SaverSearch.Application.Common.Models.Pipeline.Rules;

namespace SaverSearch.Application.Common.Interfaces;

public interface IRulesEngine
{
    Task<RuleEvaluationResult> EvaluateAsync(
        ResolvedOffer offer, 
        DiscoveryContext context, 
        CancellationToken cancellationToken = default);
}
// This interface allows the engine to be consumed and executed on any ResolvedOffer.
