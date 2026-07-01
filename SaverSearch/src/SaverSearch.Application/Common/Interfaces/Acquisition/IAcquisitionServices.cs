using SaverSearch.Application.Common.Models.Acquisition;
using SaverSearch.Domain.Entities;

namespace SaverSearch.Application.Common.Interfaces.Acquisition;

/// <summary>
/// Orchestrates the full acquisition pipeline for one or all connectors.
/// </summary>
public interface IOfferAcquisitionEngine
{
    /// <summary>Runs the pipeline for a single named connector.</summary>
    Task<ImportJobResult> RunAsync(string providerName, CancellationToken cancellationToken = default);

    /// <summary>Runs the pipeline for all registered connectors in parallel.</summary>
    Task<IEnumerable<ImportJobResult>> RunAllAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Validates a single <see cref="RawProviderOffer"/>.
/// Never throws. Collects all warnings and errors.
/// </summary>
public interface IOfferValidationService
{
    ValidationResult Validate(RawProviderOffer offer, string providerName);
}

/// <summary>
/// Maps a <see cref="RawProviderOffer"/> to a domain <see cref="Offer"/> entity.
/// No provider-specific logic. Uses only fields defined on the raw model.
/// </summary>
public interface IOfferNormalisationService
{
    Offer Normalise(RawProviderOffer raw, Guid retailerId, Guid providerId, Guid offerTypeId);
}

/// <summary>
/// Inserts new, updates changed, and soft-deletes missing offers for a provider.
/// </summary>
public interface IOfferUpsertService
{
    Task<UpsertSummary> UpsertBatchAsync(
        IEnumerable<Offer> incomingOffers,
        Guid providerId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Creates and persists <see cref="ImportJobRecord"/> entries.
/// </summary>
public interface IImportJobService
{
    Task<ImportJobRecord> CreateAsync(string providerName, string connectorVersion, CancellationToken cancellationToken = default);
    Task CompleteAsync(ImportJobRecord job, ImportJobResult result, CancellationToken cancellationToken = default);
    Task<IEnumerable<ImportJobRecord>> GetHistoryAsync(string? providerName = null, int limit = 50, CancellationToken cancellationToken = default);
}
