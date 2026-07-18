using SaverSearch.Application.Common.Models.Acquisition;

namespace SaverSearch.Infrastructure.Providers.Connectors.Awin;

/// <summary>
/// Maps internal AWIN API models to the framework-standard <see cref="RawProviderOffer"/>.
/// Contains no HTTP or I/O concerns. Stateless and unit-testable.
/// </summary>
public static class AwinOfferMapper
{
    private static readonly Dictionary<string, string> CommissionTypeMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["percentage"] = "percentage",
            ["fixed"] = "fixed",
            ["amount"] = "fixed"
        };

    /// <summary>
    /// Combines a list of AWIN promotions with their parent programmes to produce
    /// a flat collection of <see cref="RawProviderOffer"/> records.
    /// Promotions without a matching programme are skipped.
    /// </summary>
    public static IEnumerable<RawProviderOffer> Map(
        IReadOnlyList<AwinPromotion> promotions,
        IReadOnlyList<AwinProgramme> programmes)
    {
        // Build a fast lookup by advertiser ID
        var programmeIndex = programmes.ToDictionary(p => p.Id);

        foreach (var promo in promotions)
        {
            if (!programmeIndex.TryGetValue(promo.AdvertiserId, out var programme))
                continue; // No programme match — skip

            // Resolve the primary commission group (prefer "cashback" type)
            var commission = ResolvePrimaryCommission(promo.CommissionGroups);
            if (commission == null)
                continue; // No usable commission — skip

            var (value, valueType) = ExtractValue(commission);
            if (value <= 0)
                continue; // Zero or negative value — skip

            var domain = ExtractDomain(programme.DisplayUrl);
            var offerUrl = promo.OfferUrl ?? programme.ClickThroughUrl ?? programme.DisplayUrl ?? string.Empty;

            var metadata = BuildMetadata(promo, programme, commission);

            yield return new RawProviderOffer(
                ExternalId: promo.Id.ToString(),
                RetailerName: programme.Name,
                RetailerUrl: programme.DisplayUrl,
                RetailerDomain: domain,
                Title: BuildTitle(promo, programme, commission),
                Description: promo.Description?.Trim(),
                Terms: promo.Terms?.Trim(),
                OfferUrl: offerUrl,
                ValueType: valueType,
                Value: value,
                MinimumSpend: null, // AWIN does not expose this at the promotion level
                MaximumReward: commission.Amount?.Value > 0 ? commission.Amount.Value : null,
                StartDate: promo.StartDate,
                EndDate: promo.EndDate,
                IsExclusive: promo.Exclusive,
                RetrievedAt: DateTimeOffset.UtcNow,
                RawMetadata: metadata
            );
        }
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private static AwinCommissionGroup? ResolvePrimaryCommission(
        List<AwinCommissionGroup>? groups)
    {
        if (groups == null || groups.Count == 0) return null;

        // Prefer a group named "cashback" or "default", then take the first with a usable value
        return groups.FirstOrDefault(g =>
                   string.Equals(g.Name, "cashback", StringComparison.OrdinalIgnoreCase))
               ?? groups.FirstOrDefault(g =>
                   string.Equals(g.Name, "default", StringComparison.OrdinalIgnoreCase))
               ?? groups.FirstOrDefault(g => g.Percentage > 0 || g.Amount?.Value > 0);
    }

    private static (decimal Value, string ValueType) ExtractValue(AwinCommissionGroup commission)
    {
        if (commission.Percentage.HasValue && commission.Percentage > 0)
            return (commission.Percentage.Value, "percentage");

        if (commission.Amount?.Value > 0)
            return (commission.Amount.Value, "fixed");

        return (0m, "other");
    }

    private static string BuildTitle(
        AwinPromotion promo,
        AwinProgramme programme,
        AwinCommissionGroup commission)
    {
        // Use the promotion description if available; otherwise synthesise from commission
        if (!string.IsNullOrWhiteSpace(promo.Description))
            return promo.Description.Trim();

        var (value, type) = ExtractValue(commission);
        return type == "percentage"
            ? $"{value:0.##}% cashback at {programme.Name}"
            : $"£{value:0.##} cashback at {programme.Name}";
    }

    private static string? ExtractDomain(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            url = "https://" + url;

        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
            ? uri.Host.TrimStart('w', '.')
            : null;
    }

    private static IReadOnlyDictionary<string, string> BuildMetadata(
        AwinPromotion promo,
        AwinProgramme programme,
        AwinCommissionGroup commission)
    {
        var meta = new Dictionary<string, string>
        {
            ["network"] = "AWIN",
            ["advertiserId"] = promo.AdvertiserId.ToString(),
            ["promotionType"] = promo.Type ?? "unknown",
            ["commissionGroupName"] = commission.Name ?? string.Empty,
            ["commissionGroupType"] = commission.Type ?? string.Empty,
            ["sector"] = programme.PrimarySector?.Name ?? string.Empty,
            ["region"] = programme.PrimaryRegion?.Name ?? string.Empty
        };

        if (!string.IsNullOrWhiteSpace(promo.Code))
            meta["promoCode"] = promo.Code;

        if (!string.IsNullOrWhiteSpace(programme.LogoUrl))
            meta["logoUrl"] = programme.LogoUrl;

        return meta;
    }
}
