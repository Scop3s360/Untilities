using SaverSearch.Application.Common.Models.Pipeline;
using SaverSearch.Application.Common.Models.Pipeline.Planning;
using SaverSearch.Application.Common.Models.Pipeline.Recommendations;

namespace SaverSearch.Application.Common.Interfaces;

public interface IRiskEvaluator
{
    string EvaluatorName { get; }
    RiskAnalysis EvaluateRisk(PurchasePlan plan, DiscoveryContext context);
}
