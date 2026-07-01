using System.Diagnostics;
using SaverSearch.Application.Common.Interfaces;
using SaverSearch.Application.Common.Models.Pipeline;
using SaverSearch.Application.Common.Models.Pipeline.Planning;
using SaverSearch.Application.Common.Models.Pipeline.Ranking;

namespace SaverSearch.Application.Services.Pipeline.Planning;

public class ProviderCompatibilityEvaluator : ICompatibilityEvaluator
{
    public string EvaluatorName => "Provider Stacking compatibility Evaluator";

    public Task<CompatibilityEvidence> EvaluateCompatibilityAsync(RankedOffer first, RankedOffer second, DiscoveryContext context, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        var firstProvider = first.NormalisedOffer.CalculatedOffer.Offer.ProviderId;
        var secondProvider = second.NormalisedOffer.CalculatedOffer.Offer.ProviderId;

        var result = CompatibilityResult.Compatible;
        string explanation = "Offers are from different reward providers and can coexist.";

        if (firstProvider == secondProvider)
        {
            // Same provider: typically cannot stack unless terms mention stacking/boosting
            var terms1 = (first.NormalisedOffer.CalculatedOffer.Offer.Terms ?? string.Empty).ToLower();
            var terms2 = (second.NormalisedOffer.CalculatedOffer.Offer.Terms ?? string.Empty).ToLower();

            if (terms1.Contains("stackable") || terms2.Contains("stackable") || terms1.Contains("combine") || terms2.Contains("combine"))
            {
                result = CompatibilityResult.Compatible;
                explanation = $"Offers are from same provider but terms explicitly support stacking.";
            }
            else
            {
                result = CompatibilityResult.Incompatible;
                explanation = $"Incompatible: Cannot stack multiple offers from the same provider ({first.NormalisedOffer.CalculatedOffer.Offer.OfferUrl}) on a single purchase.";
            }
        }

        stopwatch.Stop();

        return Task.FromResult(new CompatibilityEvidence(
            result,
            95.0,
            explanation,
            "Provider Evaluation Rules",
            stopwatch.ElapsedMilliseconds
        ));
    }
}

public class OfferTypeCompatibilityEvaluator : ICompatibilityEvaluator
{
    public string EvaluatorName => "Offer Type compatibility Evaluator";

    public Task<CompatibilityEvidence> EvaluateCompatibilityAsync(RankedOffer first, RankedOffer second, DiscoveryContext context, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        var firstType = first.NormalisedOffer.CalculatedOffer.Offer.Title.ToLower();
        var secondType = second.NormalisedOffer.CalculatedOffer.Offer.Title.ToLower();

        var result = CompatibilityResult.Compatible;
        string explanation = "Offer types are complementary (e.g. cashback + voucher + gift card).";

        // Two vouchers/promo codes are usually incompatible
        if ((firstType.Contains("voucher") || firstType.Contains("promo code") || firstType.Contains("discount")) &&
            (secondType.Contains("voucher") || secondType.Contains("promo code") || secondType.Contains("discount")))
        {
            result = CompatibilityResult.Incompatible;
            explanation = "Incompatible: Retailers typically restrict purchases to a single promo code or voucher discount per order.";
        }

        stopwatch.Stop();

        return Task.FromResult(new CompatibilityEvidence(
            result,
            90.0,
            explanation,
            "Offer Type Compatibility Model",
            stopwatch.ElapsedMilliseconds
        ));
    }
}
