using System.Text.Json.Serialization;

namespace SaverSearch.Infrastructure.Providers.Connectors.Awin;

// ─── Programme Models ────────────────────────────────────────────────────────

/// <summary>
/// Represents a single merchant programme from the AWIN Publisher API.
/// GET /publishers/{id}/programmes?relationship=joined
/// </summary>
public sealed class AwinProgramme
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("displayUrl")]
    public string? DisplayUrl { get; set; }

    [JsonPropertyName("clickThroughUrl")]
    public string? ClickThroughUrl { get; set; }

    [JsonPropertyName("logoUrl")]
    public string? LogoUrl { get; set; }

    [JsonPropertyName("primaryRegion")]
    public AwinRegion? PrimaryRegion { get; set; }

    [JsonPropertyName("primarySector")]
    public AwinSector? PrimarySector { get; set; }

    [JsonPropertyName("relationship")]
    public string? Relationship { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}

public sealed class AwinRegion
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("countryCode")]
    public string? CountryCode { get; set; }
}

public sealed class AwinSector
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

// ─── Promotion Models ────────────────────────────────────────────────────────

/// <summary>
/// Represents a single promotion/offer from the AWIN Publisher API.
/// GET /publishers/{id}/promotions?membershipStatus=joined
/// </summary>
public sealed class AwinPromotion
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("advertiserId")]
    public int AdvertiserId { get; set; }

    [JsonPropertyName("advertiserName")]
    public string? AdvertiserName { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("startDate")]
    public DateTime? StartDate { get; set; }

    [JsonPropertyName("endDate")]
    public DateTime? EndDate { get; set; }

    [JsonPropertyName("commissionGroups")]
    public List<AwinCommissionGroup>? CommissionGroups { get; set; }

    [JsonPropertyName("regions")]
    public List<AwinRegion>? Regions { get; set; }

    [JsonPropertyName("exclusive")]
    public bool Exclusive { get; set; }

    [JsonPropertyName("terms")]
    public string? Terms { get; set; }

    [JsonPropertyName("membershipStatus")]
    public string? MembershipStatus { get; set; }

    // Derived: offer URL built from the programme's click-through URL
    public string? OfferUrl { get; set; }
}

public sealed class AwinCommissionGroup
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("percentage")]
    public decimal? Percentage { get; set; }

    [JsonPropertyName("amount")]
    public AwinCommissionAmount? Amount { get; set; }
}

public sealed class AwinCommissionAmount
{
    [JsonPropertyName("amount")]
    public decimal Value { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }
}

// ─── API Response Wrappers ───────────────────────────────────────────────────

/// <summary>Wrapper for paginated programme list responses.</summary>
public sealed class AwinProgrammeResponse
{
    [JsonPropertyName("programs")]
    public List<AwinProgramme>? Programs { get; set; }

    // Some AWIN endpoints use the top-level array directly
    public static implicit operator List<AwinProgramme>?(AwinProgrammeResponse? r) =>
        r?.Programs;
}
