using SaverSearch.Application.Common.Models.Acquisition;

namespace SaverSearch.Application.Common.Interfaces.Acquisition;

/// <summary>
/// Implemented once per external provider (e.g. TopCashback, Quidco, Chase).
/// The framework discovers all implementations automatically via DI assembly scanning.
/// </summary>
public interface IProviderConnector
{
    /// <summary>Canonical provider name. Must be unique across all connectors.</summary>
    string ProviderName { get; }

    /// <summary>Connector implementation version for diagnostics.</summary>
    string ConnectorVersion { get; }

    /// <summary>When true, the connector can fetch only new/changed offers since a given date.</summary>
    bool SupportsIncrementalImport { get; }

    /// <summary>Fetches all raw offers from the provider.</summary>
    Task<IEnumerable<RawProviderOffer>> GetOffersAsync(CancellationToken cancellationToken = default);

    /// <summary>Verifies connectivity and returns latency diagnostics.</summary>
    Task<ConnectorHealthResult> HealthCheckAsync(CancellationToken cancellationToken = default);
}
