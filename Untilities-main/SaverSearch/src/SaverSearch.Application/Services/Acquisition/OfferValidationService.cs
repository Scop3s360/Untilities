using SaverSearch.Application.Common.Interfaces.Acquisition;
using SaverSearch.Application.Common.Models.Acquisition;

namespace SaverSearch.Application.Services.Acquisition;

/// <summary>
/// Validates raw provider offers. Never throws. Collects all findings.
/// </summary>
public class OfferValidationService : IOfferValidationService
{
    private static readonly HashSet<string> KnownValueTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "percentage", "fixed", "fixedamount", "points", "cashback", "other"
        };

    private static readonly HashSet<string> PlaceholderNames =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "test", "unknown", "n/a", "placeholder", "example"
        };

    public ValidationResult Validate(RawProviderOffer offer, string providerName)
    {
        var warnings = new List<ValidationWarning>();
        var errors = new List<string>();

        // Required: ExternalId
        if (string.IsNullOrWhiteSpace(offer.ExternalId))
            errors.Add($"[{providerName}] ExternalId is required.");

        // Required: RetailerName
        if (string.IsNullOrWhiteSpace(offer.RetailerName))
            errors.Add($"[{providerName}:{offer.ExternalId}] RetailerName is required.");
        else if (PlaceholderNames.Contains(offer.RetailerName.Trim()))
            warnings.Add(new ValidationWarning(offer.ExternalId, nameof(offer.RetailerName),
                $"RetailerName '{offer.RetailerName}' appears to be a placeholder.", WarningSeverity.Warning));

        // Required: Title
        if (string.IsNullOrWhiteSpace(offer.Title))
            errors.Add($"[{providerName}:{offer.ExternalId}] Title is required.");

        // Required: OfferUrl
        if (string.IsNullOrWhiteSpace(offer.OfferUrl))
            errors.Add($"[{providerName}:{offer.ExternalId}] OfferUrl is required.");
        else if (!Uri.TryCreate(offer.OfferUrl, UriKind.Absolute, out _))
            warnings.Add(new ValidationWarning(offer.ExternalId, nameof(offer.OfferUrl),
                $"OfferUrl '{offer.OfferUrl}' is not a valid absolute URI.", WarningSeverity.Warning));

        // Value > 0
        if (offer.Value <= 0)
            errors.Add($"[{providerName}:{offer.ExternalId}] Value must be greater than zero (got {offer.Value}).");

        // ValueType is known
        if (!KnownValueTypes.Contains(offer.ValueType ?? ""))
            warnings.Add(new ValidationWarning(offer.ExternalId, nameof(offer.ValueType),
                $"ValueType '{offer.ValueType}' is unknown and will be mapped to 'Other'.", WarningSeverity.Info));

        // Date consistency
        if (offer.StartDate.HasValue && offer.EndDate.HasValue && offer.EndDate < offer.StartDate)
            errors.Add($"[{providerName}:{offer.ExternalId}] EndDate ({offer.EndDate:d}) cannot be before StartDate ({offer.StartDate:d}).");

        // Past end date warning
        if (offer.EndDate.HasValue && offer.EndDate.Value < DateTime.UtcNow)
            warnings.Add(new ValidationWarning(offer.ExternalId, nameof(offer.EndDate),
                $"EndDate {offer.EndDate:d} is in the past.", WarningSeverity.Warning));

        return new ValidationResult(errors.Count == 0, warnings, errors);
    }
}
