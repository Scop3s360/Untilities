using SaverSearch.Application.Common.Models.Pipeline;
using SaverSearch.Application.Common.Models.Pipeline.Rules;

namespace SaverSearch.Application.Common.Interfaces;

public interface IRuleEvaluator
{
    string RuleName { get; }
    string Category { get; }
    Task<RuleResult> EvaluateAsync(
        ResolvedOffer offer, 
        DiscoveryContext context, 
        CancellationToken cancellationToken = default);
}
