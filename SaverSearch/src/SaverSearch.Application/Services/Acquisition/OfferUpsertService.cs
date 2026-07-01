using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SaverSearch.Application.Common.Interfaces;
using SaverSearch.Application.Common.Interfaces.Acquisition;
using SaverSearch.Application.Common.Models.Acquisition;
using SaverSearch.Domain.Entities;

namespace SaverSearch.Application.Services.Acquisition;

/// <summary>
/// Inserts new offers, updates changed offers, and soft-deletes offers
/// that are missing from the incoming batch — all within a single DB transaction.
/// Idempotent: running the same import twice produces no additional changes.
/// </summary>
public class OfferUpsertService(
    ISaverSearchDbContext dbContext,
    ILogger<OfferUpsertService> logger) : IOfferUpsertService
{
    public async Task<UpsertSummary> UpsertBatchAsync(
        IEnumerable<Offer> incomingOffers,
        Guid providerId,
        CancellationToken cancellationToken = default)
    {
        var incoming = incomingOffers.ToList();
        int inserted = 0, updated = 0, deactivated = 0;

        // Load all existing offers for this provider (active and inactive)
        var existing = await dbContext.Offers
            .Where(o => o.ProviderId == providerId)
            .ToListAsync(cancellationToken);

        // Index existing by ExternalId for O(1) lookup
        var existingByExternalId = existing
            .Where(o => o.ExternalId != null)
            .ToDictionary(o => o.ExternalId!, StringComparer.OrdinalIgnoreCase);

        var incomingExternalIds = incoming
            .Where(o => o.ExternalId != null)
            .Select(o => o.ExternalId!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var offer in incoming)
        {
            if (offer.ExternalId == null) continue;

            if (existingByExternalId.TryGetValue(offer.ExternalId, out var existingOffer))
            {
                // Update if anything has changed
                if (HasChanged(existingOffer, offer))
                {
                    ApplyUpdate(existingOffer, offer);
                    logger.LogDebug("Offer Upsert: Updated ExternalId={ExternalId}", offer.ExternalId);
                    updated++;
                }
            }
            else
            {
                await dbContext.Offers.AddAsync(offer, cancellationToken);
                logger.LogDebug("Offer Upsert: Inserted ExternalId={ExternalId}", offer.ExternalId);
                inserted++;
            }
        }

        // Soft-delete offers missing from the incoming batch
        foreach (var existingOffer in existing.Where(o => o.IsActive && o.ExternalId != null))
        {
            if (!incomingExternalIds.Contains(existingOffer.ExternalId!))
            {
                existingOffer.IsActive = false;
                existingOffer.LastUpdated = DateTime.UtcNow;
                logger.LogDebug("Offer Upsert: Deactivated ExternalId={ExternalId}", existingOffer.ExternalId);
                deactivated++;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Offer Upsert completed for Provider {ProviderId}: +{Inserted} ~{Updated} -{Deactivated}",
            providerId, inserted, updated, deactivated);

        return new UpsertSummary(inserted, updated, deactivated);
    }

    private static bool HasChanged(Offer existing, Offer incoming) =>
        existing.Title != incoming.Title ||
        existing.Description != incoming.Description ||
        existing.Value != incoming.Value ||
        existing.ValueType != incoming.ValueType ||
        existing.MinimumSpend != incoming.MinimumSpend ||
        existing.MaximumReward != incoming.MaximumReward ||
        existing.StartDate != incoming.StartDate ||
        existing.EndDate != incoming.EndDate ||
        existing.Terms != incoming.Terms ||
        existing.OfferUrl != incoming.OfferUrl ||
        existing.IsExclusive != incoming.IsExclusive ||
        !existing.IsActive; // re-activate if it was previously deactivated

    private static void ApplyUpdate(Offer target, Offer source)
    {
        target.Title = source.Title;
        target.Description = source.Description;
        target.Value = source.Value;
        target.ValueType = source.ValueType;
        target.MinimumSpend = source.MinimumSpend;
        target.MaximumReward = source.MaximumReward;
        target.StartDate = source.StartDate;
        target.EndDate = source.EndDate;
        target.Terms = source.Terms;
        target.OfferUrl = source.OfferUrl;
        target.IsExclusive = source.IsExclusive;
        target.IsActive = true;
        target.LastUpdated = DateTime.UtcNow;
    }
}
