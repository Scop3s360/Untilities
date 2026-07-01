namespace SaverSearch.Application.Common.Models.Pipeline;

public record DiscoveryContext(
    Guid? UserId,
    string RawQuery,
    string? RetailerSlug,
    decimal TargetSpend,
    string? PaymentMethod,
    string? UserCardTier,
    string? UserRegion,
    IDictionary<string, string> Preferences
);
