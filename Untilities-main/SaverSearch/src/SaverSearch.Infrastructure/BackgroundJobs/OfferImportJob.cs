using Microsoft.Extensions.Logging;
using SaverSearch.Application.Common.Interfaces.Acquisition;

namespace SaverSearch.Infrastructure.BackgroundJobs;

/// <summary>
/// Background job that triggers the Offer Acquisition Engine for all registered connectors.
/// Scheduled to run on a recurring basis (e.g. daily via Hangfire/Quartz in a future phase).
/// </summary>
public class OfferImportJob(
    IOfferAcquisitionEngine acquisitionEngine,
    ILogger<OfferImportJob> logger)
{
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("OfferImportJob: Starting acquisition run for all providers.");

        try
        {
            var results = (await acquisitionEngine.RunAllAsync(cancellationToken)).ToList();

            logger.LogInformation(
                "OfferImportJob: Completed {Count} provider(s). Summary: {Summary}",
                results.Count,
                string.Join(", ", results.Select(r =>
                    $"{r.ProviderName}={r.Status()}")));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "OfferImportJob: Acquisition run failed.");
            throw;
        }
    }
}

file static class ImportJobResultExtensions
{
    public static string Status(this SaverSearch.Application.Common.Models.Acquisition.ImportJobResult r) =>
        r.Success ? $"+{r.OffersAdded}~{r.OffersUpdated}-{r.OffersDeactivated}" : $"FAILED({r.ErrorMessage})";
}
