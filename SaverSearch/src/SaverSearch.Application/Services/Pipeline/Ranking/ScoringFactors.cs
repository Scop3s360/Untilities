using SaverSearch.Application.Common.Interfaces;
using SaverSearch.Application.Common.Models.Pipeline;
using SaverSearch.Application.Common.Models.Pipeline.Normalisation;

namespace SaverSearch.Application.Services.Pipeline.Ranking;

public class ExpectedValueFactor : IScoringFactor
{
    public string FactorName => "Expected Value Factor";

    public double CalculateScore(NormalisedOffer offer, DiscoveryContext context)
    {
        return (double)offer.ExpectedMonetaryValue;
    }
}

public class GuaranteedValueFactor : IScoringFactor
{
    public string FactorName => "Guaranteed Value Factor";

    public double CalculateScore(NormalisedOffer offer, DiscoveryContext context)
    {
        return (double)offer.GuaranteedMonetaryValue;
    }
}

public class ConfidenceScoreFactor : IScoringFactor
{
    public string FactorName => "Confidence Score Factor";

    public double CalculateScore(NormalisedOffer offer, DiscoveryContext context)
    {
        return offer.ConfidenceScore;
    }
}

public class ComplexityScoreFactor : IScoringFactor
{
    public string FactorName => "Complexity Score Factor";

    public double CalculateScore(NormalisedOffer offer, DiscoveryContext context)
    {
        // Lower complexity means higher simplicity. Let's estimate a simplicity score between 0.0 and 100.0.
        // Direct cashback is simplest (100 score). Gift Cards require steps (20 score). Vouchers require code entry (50 score).
        var terms = (offer.CalculatedOffer.Offer.Terms ?? string.Empty).ToLower();
        var title = (offer.CalculatedOffer.Offer.Title ?? string.Empty).ToLower();

        double complexityPenalty = 0.0;

        if (terms.Contains("gift card") || title.Contains("gift card"))
        {
            complexityPenalty = 80.0;
        }
        else if (terms.Contains("voucher") || title.Contains("voucher") || terms.Contains("promo code"))
        {
            complexityPenalty = 50.0;
        }
        else if (terms.Contains("portal") || terms.Contains("redirect"))
        {
            complexityPenalty = 20.0;
        }

        return 100.0 - complexityPenalty;
    }
}
