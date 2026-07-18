using System.Diagnostics;
using SaverSearch.Application.Common.Interfaces;
using SaverSearch.Application.Common.Models.Pipeline;
using SaverSearch.Application.Common.Models.Pipeline.Calculations;
using SaverSearch.Application.Common.Models.Pipeline.Normalisation;

namespace SaverSearch.Application.Services.Pipeline;

public class OfferNormalisationEngine(IEnumerable<INormalisationStrategy> strategies) : IOfferNormalisationEngine
{
    private readonly List<INormalisationStrategy> _strategies = strategies.ToList();

    public async Task<IEnumerable<NormalisedOffer>> NormaliseOffersAsync(IEnumerable<CalculatedOffer> calculatedOffers, DiscoveryContext context, CancellationToken cancellationToken = default)
    {
        var tasks = calculatedOffers.Select(async calculatedOffer =>
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return CreateFallbackNormalisedOffer(calculatedOffer);
            }

            var strategy = _strategies.FirstOrDefault(s => s.CanNormalise(calculatedOffer));
            if (strategy != null)
            {
                try
                {
                    return await strategy.NormaliseAsync(calculatedOffer, context, cancellationToken);
                }
                catch
                {
                    return CreateFallbackNormalisedOffer(calculatedOffer);
                }
            }

            return CreateFallbackNormalisedOffer(calculatedOffer);
        });

        return await Task.WhenAll(tasks);
    }

    private NormalisedOffer CreateFallbackNormalisedOffer(CalculatedOffer calculatedOffer)
    {
        var steps = new List<NormalisationStep>
        {
            new NormalisationStep($"Original Flat Value: {calculatedOffer.FinalSaving:C}", calculatedOffer.FinalSaving),
            new NormalisationStep("Fallback direct mapping applied.", calculatedOffer.FinalSaving)
        };

        var diagnostics = new NormalisationDiagnostics(
            0,
            "Fallback Normalisation Strategy",
            "Calculated Offer",
            new List<string> { "Fallback normalisation strategy used." }
        );

        return new NormalisedOffer(
            calculatedOffer,
            calculatedOffer.FinalSaving,
            calculatedOffer.FinalSaving,
            calculatedOffer.FinalSaving,
            calculatedOffer.EffectiveSavingPercentage,
            80.0, // Default fallback confidence score
            "Direct Mapping",
            "Fallback Normalisation Strategy",
            steps,
            diagnostics
        );
    }
}
