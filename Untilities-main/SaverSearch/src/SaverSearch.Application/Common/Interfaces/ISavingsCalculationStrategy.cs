using SaverSearch.Application.Common.Models.Pipeline;
using SaverSearch.Application.Common.Models.Pipeline.Calculations;
using SaverSearch.Application.Dtos;

namespace SaverSearch.Application.Common.Interfaces;

public interface ISavingsCalculationStrategy
{
    string StrategyName { get; }
    bool CanCalculate(OfferDto offer);
    Task<CalculatedOffer> CalculateAsync(
        ResolvedOffer resolvedOffer, 
        DiscoveryContext context, 
        CancellationToken cancellationToken = default);
}
