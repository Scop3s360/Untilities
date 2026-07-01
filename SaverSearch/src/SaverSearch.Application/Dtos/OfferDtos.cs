using SaverSearch.Domain.Entities;

namespace SaverSearch.Application.Dtos;

public record OfferDto(
    Guid Id,
    Guid RetailerId,
    Guid ProviderId,
    Guid OfferTypeId,
    string Title,
    string? Description,
    decimal Value,
    OfferValueType ValueType,
    decimal? MinimumSpend,
    decimal? MaximumReward,
    DateTime? StartDate,
    DateTime? EndDate,
    string? Terms,
    string OfferUrl,
    bool IsExclusive,
    bool IsActive,
    DateTime LastUpdated
);

public record CreateOfferDto(
    Guid RetailerId,
    Guid ProviderId,
    Guid OfferTypeId,
    string Title,
    string? Description,
    decimal Value,
    OfferValueType ValueType,
    decimal? MinimumSpend,
    decimal? MaximumReward,
    DateTime? StartDate,
    DateTime? EndDate,
    string? Terms,
    string OfferUrl,
    bool IsExclusive = false,
    bool IsActive = true
);

public record UpdateOfferDto(
    Guid RetailerId,
    Guid ProviderId,
    Guid OfferTypeId,
    string Title,
    string? Description,
    decimal Value,
    OfferValueType ValueType,
    decimal? MinimumSpend,
    decimal? MaximumReward,
    DateTime? StartDate,
    DateTime? EndDate,
    string? Terms,
    string OfferUrl,
    bool IsExclusive,
    bool IsActive
);
