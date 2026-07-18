using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SaverSearch.Application.Common.Interfaces;
using SaverSearch.Application.Common.Interfaces.Acquisition;
using SaverSearch.Application.Common.Models.Acquisition;
using SaverSearch.Domain.Entities;

namespace SaverSearch.Application.Services.Acquisition;

/// <summary>
/// Creates and persists <see cref="ImportJobRecord"/> acquisition history entries.
/// </summary>
public class ImportJobService(
    ISaverSearchDbContext dbContext,
    ILogger<ImportJobService> logger) : IImportJobService
{
    public async Task<ImportJobRecord> CreateAsync(
        string providerName,
        string connectorVersion,
        CancellationToken cancellationToken = default)
    {
        var job = new ImportJobRecord
        {
            ProviderName = providerName,
            ConnectorVersion = connectorVersion,
            Status = ImportJobStatus.Running,
            StartedAt = DateTime.UtcNow
        };

        await dbContext.ImportJobs.AddAsync(job, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("ImportJob created: {JobId} for provider {ProviderName}", job.Id, providerName);
        return job;
    }

    public async Task CompleteAsync(
        ImportJobRecord job,
        ImportJobResult result,
        CancellationToken cancellationToken = default)
    {
        job.Status = result.Success ? ImportJobStatus.Completed : ImportJobStatus.Failed;
        job.CompletedAt = result.CompletedAt;
        job.DurationMs = result.DurationMs;
        job.OffersDownloaded = result.OffersDownloaded;
        job.OffersValidated = result.OffersValidated;
        job.OffersAdded = result.OffersAdded;
        job.OffersUpdated = result.OffersUpdated;
        job.OffersDeactivated = result.OffersDeactivated;
        job.ValidationWarningCount = result.ValidationWarningCount;
        job.ErrorMessage = result.ErrorMessage;
        job.Warnings = JsonSerializer.Serialize(result.Warnings);

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "ImportJob completed: {JobId} Status={Status} +{Added} ~{Updated} -{Deactivated}",
            job.Id, job.Status, job.OffersAdded, job.OffersUpdated, job.OffersDeactivated);
    }

    public async Task<IEnumerable<ImportJobRecord>> GetHistoryAsync(
        string? providerName = null,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.ImportJobs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(providerName))
            query = query.Where(j => j.ProviderName == providerName);

        return await query
            .OrderByDescending(j => j.StartedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}
