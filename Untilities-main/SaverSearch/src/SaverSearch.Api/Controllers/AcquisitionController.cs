using Microsoft.AspNetCore.Mvc;
using SaverSearch.Application.Common.Interfaces.Acquisition;
using SaverSearch.Application.Common.Models;
using SaverSearch.Application.Common.Models.Acquisition;

namespace SaverSearch.Api.Controllers;

/// <summary>
/// Provides REST endpoints for the Offer Acquisition Framework.
///
/// Validation-phase endpoints only — not intended for production scheduling.
/// For production, trigger via the background job scheduler (e.g. Hangfire/Quartz).
/// </summary>
[Produces("application/json")]
public class AcquisitionController(
    IOfferAcquisitionEngine acquisitionEngine,
    IImportJobService importJobService,
    IEnumerable<IProviderConnector> connectors) : BaseApiController
{
    // ── Health Check ─────────────────────────────────────────────────────────

    /// <summary>
    /// Runs the health check for a named connector and returns live diagnostics.
    /// </summary>
    /// <param name="providerName">
    /// The canonical provider name (case-insensitive). Example: <c>AWIN</c>
    /// </param>
    [HttpGet("health/{providerName}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<ConnectorHealthResult>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ApiResponse<object>))]
    public async Task<ActionResult<ApiResponse<ConnectorHealthResult>>> HealthCheck(
        string providerName,
        CancellationToken cancellationToken)
    {
        var connector = connectors.FirstOrDefault(c =>
            string.Equals(c.ProviderName, providerName, StringComparison.OrdinalIgnoreCase));

        if (connector is null)
            return NotFound(ApiResponse<object>.ErrorResponse($"No connector registered for provider '{providerName}'."));

        var result = await connector.HealthCheckAsync(cancellationToken);
        return Ok(ApiResponse<ConnectorHealthResult>.SuccessResponse(result,
            result.IsHealthy ? "Connector is healthy." : "Connector health check failed."));
    }

    /// <summary>
    /// Returns the health status of all registered connectors simultaneously.
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<IEnumerable<ConnectorHealthResult>>))]
    public async Task<ActionResult<ApiResponse<IEnumerable<ConnectorHealthResult>>>> HealthCheckAll(
        CancellationToken cancellationToken)
    {
        var tasks = connectors.Select(c => c.HealthCheckAsync(cancellationToken));
        var results = await Task.WhenAll(tasks);
        return Ok(ApiResponse<IEnumerable<ConnectorHealthResult>>.SuccessResponse(results,
            $"{results.Length} connector(s) checked."));
    }

    // ── Import Trigger ───────────────────────────────────────────────────────

    /// <summary>
    /// Triggers a full offer import pipeline for a single named connector.
    /// Returns import statistics including offers added, updated, and deactivated.
    /// </summary>
    /// <param name="providerName">
    /// The canonical provider name (case-insensitive). Example: <c>AWIN</c>
    /// </param>
    /// <remarks>
    /// This is a synchronous, long-running operation. Expect 30–120 seconds for live connectors.
    /// For production use, trigger via the background job scheduler instead.
    /// Idempotency: Running twice produces zero duplicates on the second run.
    /// </remarks>
    [HttpPost("import/{providerName}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<ImportJobResult>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ApiResponse<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ApiResponse<object>))]
    public async Task<ActionResult<ApiResponse<ImportJobResult>>> TriggerImport(
        string providerName,
        CancellationToken cancellationToken)
    {
        var connector = connectors.FirstOrDefault(c =>
            string.Equals(c.ProviderName, providerName, StringComparison.OrdinalIgnoreCase));

        if (connector is null)
            return NotFound(ApiResponse<object>.ErrorResponse($"No connector registered for provider '{providerName}'."));

        try
        {
            var result = await acquisitionEngine.RunAsync(providerName, cancellationToken);
            var message = result.Success
                ? $"Import completed. +{result.OffersAdded} ~{result.OffersUpdated} -{result.OffersDeactivated} in {result.DurationMs}ms."
                : $"Import failed: {result.ErrorMessage}";

            return Ok(ApiResponse<ImportJobResult>.SuccessResponse(result, message));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ApiResponse<object>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Triggers a full import for ALL registered, enabled connectors in parallel.
    /// </summary>
    [HttpPost("import")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<IEnumerable<ImportJobResult>>))]
    public async Task<ActionResult<ApiResponse<IEnumerable<ImportJobResult>>>> TriggerImportAll(
        CancellationToken cancellationToken)
    {
        var results = (await acquisitionEngine.RunAllAsync(cancellationToken)).ToList();
        var successCount = results.Count(r => r.Success);
        return Ok(ApiResponse<IEnumerable<ImportJobResult>>.SuccessResponse(results,
            $"{successCount}/{results.Count} connector(s) succeeded."));
    }

    // ── Import History ───────────────────────────────────────────────────────

    /// <summary>
    /// Retrieves the import job history for a specific provider.
    /// Use this to verify idempotency across multiple import runs.
    /// </summary>
    /// <param name="providerName">Filter by provider name. Leave empty to retrieve all history.</param>
    /// <param name="limit">Maximum number of records to return. Default: 20.</param>
    [HttpGet("history")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<IEnumerable<ImportJobHistoryDto>>))]
    public async Task<ActionResult<ApiResponse<IEnumerable<ImportJobHistoryDto>>>> GetImportHistory(
        [FromQuery] string? providerName,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var jobs = await importJobService.GetHistoryAsync(providerName, limit, cancellationToken);

        var dtos = jobs.Select(j => new ImportJobHistoryDto(
            Id: j.Id,
            ProviderName: j.ProviderName,
            ConnectorVersion: j.ConnectorVersion,
            StartedAt: j.StartedAt,
            CompletedAt: j.CompletedAt,
            DurationMs: j.DurationMs,
            OffersDownloaded: j.OffersDownloaded,
            OffersAdded: j.OffersAdded,
            OffersUpdated: j.OffersUpdated,
            OffersDeactivated: j.OffersDeactivated,
            Status: j.Status.ToString(),
            ErrorMessage: j.ErrorMessage
        ));

        return Ok(ApiResponse<IEnumerable<ImportJobHistoryDto>>.SuccessResponse(dtos));
    }

    /// <summary>
    /// Returns all registered connector names and their version information.
    /// </summary>
    [HttpGet("connectors")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<IEnumerable<ConnectorInfoDto>>))]
    public ActionResult<ApiResponse<IEnumerable<ConnectorInfoDto>>> ListConnectors()
    {
        var info = connectors.Select(c => new ConnectorInfoDto(
            ProviderName: c.ProviderName,
            ConnectorVersion: c.ConnectorVersion,
            SupportsIncrementalImport: c.SupportsIncrementalImport
        ));

        return Ok(ApiResponse<IEnumerable<ConnectorInfoDto>>.SuccessResponse(info,
            $"{connectors.Count()} connector(s) registered."));
    }
}

// ── Response DTOs ─────────────────────────────────────────────────────────────

/// <summary>Summary view of a completed import job for history reporting.</summary>
public record ImportJobHistoryDto(
    Guid Id,
    string ProviderName,
    string ConnectorVersion,
    DateTime StartedAt,
    DateTime? CompletedAt,
    long DurationMs,
    int OffersDownloaded,
    int OffersAdded,
    int OffersUpdated,
    int OffersDeactivated,
    string Status,
    string? ErrorMessage
);

/// <summary>Descriptor for a registered provider connector.</summary>
public record ConnectorInfoDto(
    string ProviderName,
    string ConnectorVersion,
    bool SupportsIncrementalImport
);
