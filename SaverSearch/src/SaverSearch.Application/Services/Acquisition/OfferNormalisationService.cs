using SaverSearch.Application.Common.Interfaces.Acquisition;
using SaverSearch.Application.Common.Models.Acquisition;
using SaverSearch.Domain.Entities;

namespace SaverSearch.Application.Services.Acquisition;

/// <summary>
/// Maps a <see cref="RawProviderOffer"/> to a domain <see cref="Offer"/> entity.
/// Contains no provider-specific logic. All provider fields must already
/// be normalised into the raw model by the connector.
/// </summary>
public class OfferNormalisationService : IOfferNormalisationService
{
    private static readonly Dictionary<string, OfferValueType> ValueTypeMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["percentage"] = OfferValueType.Percentage,
            ["cashback"] = OfferValueType.Percentage,
            ["fixed"] = OfferValueType.FixedAmount,
            ["fixedamount"] = OfferValueType.FixedAmount,
            ["points"] = OfferValueType.Points,
            ["other"] = OfferValueType.Other
        };

    public Offer Normalise(RawProviderOffer raw, Guid retailerId, Guid providerId, Guid offerTypeId)
    {
        return new Offer
        {
            RetailerId = retailerId,
            ProviderId = providerId,
            OfferTypeId = offerTypeId,
            ExternalId = raw.ExternalId,
            Title = raw.Title.Trim(),
            Description = raw.Description?.Trim(),
            Terms = raw.Terms?.Trim(),
            OfferUrl = raw.OfferUrl.Trim(),
            Value = raw.Value,
            ValueType = MapValueType(raw.ValueType),
            MinimumSpend = raw.MinimumSpend,
            MaximumReward = raw.MaximumReward,
            StartDate = raw.StartDate,
            EndDate = raw.EndDate,
            IsExclusive = raw.IsExclusive,
            IsActive = true,
            LastUpdated = DateTime.UtcNow
        };
    }

    private static OfferValueType MapValueType(string? rawType) =>
        rawType != null && ValueTypeMap.TryGetValue(rawType, out var mapped)
            ? mapped
            : OfferValueType.Other;
}
