namespace SaverSearch.Application.Common.Models.Acquisition;

/// <summary>
/// A single offer exactly as received from an external provider.
/// No mapping or interpretation is performed here.
/// </summary>
public record RawProviderOffer(
    /// <summary>Provider's own canonical identifier for this offer. Used as the deduplication key.</summary>
    string ExternalId,
    string RetailerName,
    string? RetailerUrl,
    string? RetailerDomain,
    string Title,
    string? Description,
    string? Terms,
    string OfferUrl,
    /// <summary>Raw value type string from the provider (e.g. "percentage", "fixed", "points").</summary>
    string ValueType,
    decimal Value,
    decimal? MinimumSpend,
    decimal? MaximumReward,
    DateTime? StartDate,
    DateTime? EndDate,
    bool IsExclusive,
    DateTimeOffset RetrievedAt,
    IReadOnlyDictionary<string, string> RawMetadata
);

/// <summary>
/// Retailer information as provided by an external connector.
/// </summary>
public record RawProviderRetailer(
    string Name,
    string? Domain,
    string? Website,
    string? LogoUrl
);

/// <summary>
/// Metadata describing the connector run that produced raw offers.
/// </summary>
public record RawProviderMetadata(
    string ProviderName,
    string ConnectorVersion,
    DateTimeOffset RetrievedAt,
    string? SourceUrl,
    IReadOnlyDictionary<string, string> CustomFields
);

/// <summary>
/// Result of a connector health check.
/// </summary>
public record ConnectorHealthResult(
    bool IsHealthy,
    string ProviderName,
    string ConnectorVersion,
    DateTimeOffset CheckedAt,
    long LatencyMs,
    string? Message
);
