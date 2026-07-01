using SaverSearch.Application.Common.Models.Pipeline;
using SaverSearch.Application.Common.Models.Pipeline.Calculations;

namespace SaverSearch.Application.Common.Interfaces;

public interface ISavingsCalculator
{
    Task<CalculatedOffer> CalculateAsync(
        ResolvedOffer resolvedOffer, 
        DiscoveryContext context, 
        CancellationToken cancellationToken = default);
}
