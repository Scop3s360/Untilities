using SaverSearch.Application.Common.Models.Pipeline;
using SaverSearch.Application.Common.Models.Pipeline.Normalisation;

namespace SaverSearch.Application.Common.Interfaces;

public interface IScoringFactor
{
    string FactorName { get; }
    double CalculateScore(NormalisedOffer offer, DiscoveryContext context);
}
