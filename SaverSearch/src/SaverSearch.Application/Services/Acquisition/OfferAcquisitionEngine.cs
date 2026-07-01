using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SaverSearch.Application.Common.Interfaces;
using SaverSearch.Application.Common.Interfaces.Acquisition;
using SaverSearch.Application.Common.Models.Acquisition;
using SaverSearch.Application.Common.Models.Pipeline;

namespace SaverSearch.Application.Services.Acquisition;

/// <summary>
/// Orchestrates the full offer acquisition pipeline for one or all provider connectors.
///
/// Pipeline per connector:
///   1. Download raw offers
///   2. Validate (collect warnings, discard invalids)
///   3. Normalise raw → Offer entity
///   4. Retailer resolution (skip with warning if unknown)
///   5. Upsert (insert / update / soft-delete)
///   6. Persist ImportJobRecord
/// </summary>
public class OfferAcquisitionEngine(
    IEnumerable<IProviderConnector> connectors,
    IOfferValidationService validationService,
    IOfferNormalisationService normalisationService,
    IOfferUpsertService upsertService,
    IImportJobService importJobService,
    ISaverSearchDbContext dbContext,
    ILogger<OfferAcquisitionEngine> logger) : IOfferAcquisitionEngine
{
    private readonly List<IProviderConnector> _connectors = connectors.ToList();

    public async Task<ImportJobResult> RunAsync(
        string providerName,
        CancellationToken cancellationToken = default)
    {
        var connector = _connectors.FirstOrDefault(c =>
            string.Equals(c.ProviderName, providerName, StringComparison.OrdinalIgnoreCase));

        if (connector == null)
            throw new InvalidOperationException($"No connector registered for provider '{providerName}'.");

        return await ExecutePipelineAsync(connector, cancellationToken);
    }

    public async Task<IEnumerable<ImportJobResult>> RunAllAsync(
        CancellationToken cancellationToken = default)
    {
        if (_connectors.Count == 0)
        {
            logger.LogWarning("OfferAcquisitionEngine: No connectors registered.");
            return [];
        }

        logger.LogInformation("OfferAcquisitionEngine: Running {Count} connectors in parallel.", _connectors.Count);

        var tasks = _connectors.Select(c => ExecutePipelineAsync(c, cancellationToken));
        return await Task.WhenAll(tasks);
    }

    // ──────────────────────────────────────────────
    // Core pipeline
    // ──────────────────────────────────────────────

    private async Task<ImportJobResult> ExecutePipelineAsync(
        IProviderConnector connector,
        CancellationToken cancellationToken)
    {
        var jobStart = DateTime.UtcNow;
        var overallSw = Stopwatch.StartNew();
        var stageTimings = new List<StageTiming>();
        var warnings = new List<string>();

        logger.LogInformation(
            "OfferAcquisitionEngine: Starting pipeline for {ProviderName} v{Version}",
            connector.ProviderName, connector.ConnectorVersion);

        var job = await importJobService.CreateAsync(
            connector.ProviderName, connector.ConnectorVersion, cancellationToken);

        try
        {
            // ── Stage 1: Download ──
            var downloadSw = Stopwatch.StartNew();
            IEnumerable<RawProviderOffer> rawOffers;

            try
            {
                rawOffers = (await connector.GetOffersAsync(cancellationToken)).ToList();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "OfferAcquisitionEngine: Download failed for {ProviderName}", connector.ProviderName);
                return await FailJobAsync(job, $"Download failed: {ex.Message}", stageTimings, warnings, jobStart, cancellationToken);
            }

            downloadSw.Stop();
            stageTimings.Add(new StageTiming("Download", downloadSw.ElapsedMilliseconds));
            var offersDownloaded = rawOffers.Count();
            logger.LogInformation("OfferAcquisitionEngine: Downloaded {Count} offers.", offersDownloaded);

            // ── Stage 2: Validate ──
            var validateSw = Stopwatch.StartNew();
            var validOffers = new List<RawProviderOffer>();

            foreach (var raw in rawOffers)
            {
                var result = validationService.Validate(raw, connector.ProviderName);
                foreach (var w in result.Warnings)
                    warnings.Add($"[{w.ExternalId}] {w.Field}: {w.Message}");

                if (result.IsValid)
                    validOffers.Add(raw);
                else
                    foreach (var e in result.Errors)
                        warnings.Add($"INVALID: {e}");
            }

            validateSw.Stop();
            stageTimings.Add(new StageTiming("Validation", validateSw.ElapsedMilliseconds));
            logger.LogInformation("OfferAcquisitionEngine: {Valid} valid / {Invalid} invalid offers.",
                validOffers.Count, offersDownloaded - validOffers.Count);

            // ── Stage 3 + 4: Normalise + Retailer Resolution ──
            var normSw = Stopwatch.StartNew();
            var provider = await dbContext.Providers
                .FirstOrDefaultAsync(p => p.Name == connector.ProviderName, cancellationToken);

            if (provider == null)
            {
                logger.LogWarning(
                    "OfferAcquisitionEngine: No Provider entity found for '{ProviderName}'. Creating a placeholder.",
                    connector.ProviderName);

                provider = new SaverSearch.Domain.Entities.Provider
                {
                    Name = connector.ProviderName,
                    Website = $"https://{connector.ProviderName.ToLowerInvariant()}.com",
                    IsActive = true
                };
                await dbContext.Providers.AddAsync(provider, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            // Resolve OfferType — use first available type as default fallback
            var defaultOfferType = await dbContext.OfferTypes
                .FirstOrDefaultAsync(cancellationToken);

            if (defaultOfferType == null)
            {
                return await FailJobAsync(
                    job, "No OfferType found in the database. Seed at least one OfferType before running imports.",
                    stageTimings, warnings, jobStart, cancellationToken);
            }

            var normalisedOffers = new List<SaverSearch.Domain.Entities.Offer>();

            foreach (var raw in validOffers)
            {
                // Retailer resolution by slug/domain — skip with warning if not found
                var retailer = await ResolveRetailerAsync(raw, cancellationToken);
                if (retailer == null)
                {
                    warnings.Add($"[{raw.ExternalId}] Retailer '{raw.RetailerName}' not found in database. Offer skipped.");
                    continue;
                }

                var offer = normalisationService.Normalise(raw, retailer.Id, provider.Id, defaultOfferType.Id);
                normalisedOffers.Add(offer);
            }

            normSw.Stop();
            stageTimings.Add(new StageTiming("Normalisation + Retailer Resolution", normSw.ElapsedMilliseconds));

            // ── Stage 5: Upsert ──
            var upsertSw = Stopwatch.StartNew();
            var upsertSummary = await upsertService.UpsertBatchAsync(normalisedOffers, provider.Id, cancellationToken);
            upsertSw.Stop();
            stageTimings.Add(new StageTiming("Upsert", upsertSw.ElapsedMilliseconds));

            overallSw.Stop();
            var jobEnd = DateTime.UtcNow;

            var jobResult = new ImportJobResult(
                JobId: job.Id,
                ProviderName: connector.ProviderName,
                ConnectorVersion: connector.ConnectorVersion,
                Success: true,
                StartedAt: jobStart,
                CompletedAt: jobEnd,
                DurationMs: overallSw.ElapsedMilliseconds,
                OffersDownloaded: offersDownloaded,
                OffersValidated: validOffers.Count,
                OffersAdded: upsertSummary.Inserted,
                OffersUpdated: upsertSummary.Updated,
                OffersDeactivated: upsertSummary.Deactivated,
                ValidationWarningCount: warnings.Count,
                ErrorMessage: null,
                Warnings: warnings,
                StageTimings: stageTimings
            );

            await importJobService.CompleteAsync(job, jobResult, cancellationToken);

            logger.LogInformation(
                "OfferAcquisitionEngine: Completed {ProviderName} in {DurationMs}ms. +{Added} ~{Updated} -{Deactivated}",
                connector.ProviderName, overallSw.ElapsedMilliseconds,
                upsertSummary.Inserted, upsertSummary.Updated, upsertSummary.Deactivated);

            return jobResult;
        }
        catch (OperationCanceledException)
        {
            overallSw.Stop();
            logger.LogWarning("OfferAcquisitionEngine: Pipeline cancelled for {ProviderName}.", connector.ProviderName);
            return await FailJobAsync(job, "Operation was cancelled.", stageTimings, warnings, jobStart, cancellationToken);
        }
        catch (Exception ex)
        {
            overallSw.Stop();
            logger.LogError(ex, "OfferAcquisitionEngine: Unexpected failure for {ProviderName}.", connector.ProviderName);
            return await FailJobAsync(job, ex.Message, stageTimings, warnings, jobStart, cancellationToken);
        }
    }

    private async Task<SaverSearch.Domain.Entities.Retailer?> ResolveRetailerAsync(
        RawProviderOffer raw,
        CancellationToken cancellationToken)
    {
        // Try match by domain first, then by name (case-insensitive)
        var normalised = raw.RetailerName.Trim().ToLowerInvariant();

        return await dbContext.Retailers
            .FirstOrDefaultAsync(r =>
                r.IsActive && (
                    (raw.RetailerDomain != null && r.Website.Contains(raw.RetailerDomain)) ||
                    r.Name.ToLower() == normalised ||
                    r.Slug.ToLower() == normalised),
                cancellationToken);
    }

    private async Task<ImportJobResult> FailJobAsync(
        SaverSearch.Domain.Entities.ImportJobRecord job,
        string error,
        List<StageTiming> stageTimings,
        List<string> warnings,
        DateTime startedAt,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var result = new ImportJobResult(
            JobId: job.Id,
            ProviderName: job.ProviderName,
            ConnectorVersion: job.ConnectorVersion,
            Success: false,
            StartedAt: startedAt,
            CompletedAt: now,
            DurationMs: (long)(now - startedAt).TotalMilliseconds,
            OffersDownloaded: 0,
            OffersValidated: 0,
            OffersAdded: 0,
            OffersUpdated: 0,
            OffersDeactivated: 0,
            ValidationWarningCount: warnings.Count,
            ErrorMessage: error,
            Warnings: warnings,
            StageTimings: stageTimings
        );

        try
        {
            await importJobService.CompleteAsync(job, result, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "OfferAcquisitionEngine: Failed to persist failure record for {ProviderName}.", job.ProviderName);
        }

        return result;
    }
}
