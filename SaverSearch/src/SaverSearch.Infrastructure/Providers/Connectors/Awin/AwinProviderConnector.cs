using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaverSearch.Application.Common.Interfaces.Acquisition;
using SaverSearch.Application.Common.Models.Acquisition;

namespace SaverSearch.Infrastructure.Providers.Connectors.Awin;

/// <summary>
/// The AWIN Publisher API connector.
/// Implements <see cref="IProviderConnector"/> and integrates with the Offer Acquisition Framework.
///
/// Data flow:
///   1. Fetch all joined merchant programmes (GB region)
///   2. Fetch all active promotions (joined programmes, GB region)
///   3. Map AWIN response models → <see cref="RawProviderOffer"/>
///
/// This connector is auto-discovered by the DI assembly scan in Infrastructure DependencyInjection.
/// It is disabled by default. Set Acquisition:Awin:Enabled = true once credentials are configured.
/// </summary>
public sealed class AwinProviderConnector(
    AwinApiClient apiClient,
    IOptions<AwinConnectorOptions> options,
    ILogger<AwinProviderConnector> logger) : IProviderConnector
{
    private readonly AwinConnectorOptions _options = options.Value;

    public string ProviderName => "AWIN";
    public string ConnectorVersion => "1.0.0";
    public bool SupportsIncrementalImport => false; // Future: true when AWIN exposes sinceDate filter

    public async Task<IEnumerable<RawProviderOffer>> GetOffersAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured)
        {
            logger.LogWarning(
                "AWIN connector is not configured (PublisherId or AccessToken missing). " +
                "Set Acquisition:Awin:PublisherId and Acquisition:Awin:AccessToken in configuration.");
            return [];
        }

        logger.LogInformation("AWIN: Starting offer retrieval for publisher {PublisherId}", _options.PublisherId);

        // Stage 1: Fetch all joined programmes
        var programmes = await apiClient.GetJoinedProgrammesAsync(cancellationToken);
        logger.LogInformation("AWIN: Retrieved {Count} joined programmes.", programmes.Count);

        if (programmes.Count == 0)
        {
            logger.LogWarning("AWIN: No joined programmes found. Ensure you have applied to merchant programmes in the AWIN dashboard.");
            return [];
        }

        // Stage 2: Fetch all active promotions
        var promotions = await apiClient.GetActivePromotionsAsync(cancellationToken);
        logger.LogInformation("AWIN: Retrieved {Count} active promotions.", promotions.Count);

        // Stage 3: Map to RawProviderOffer
        var offers = AwinOfferMapper.Map(promotions, programmes).ToList();
        logger.LogInformation("AWIN: Mapped {Count} offers from {Promotions} promotions.", offers.Count, promotions.Count);

        return offers;
    }

    public async Task<ConnectorHealthResult> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        var checkedAt = DateTimeOffset.UtcNow;

        if (!_options.IsConfigured)
        {
            return new ConnectorHealthResult(
                IsHealthy: false,
                ProviderName: ProviderName,
                ConnectorVersion: ConnectorVersion,
                CheckedAt: checkedAt,
                LatencyMs: 0,
                Message: "AWIN connector is not configured. Set PublisherId and AccessToken.");
        }

        var (isHealthy, latencyMs, errorMessage) = await apiClient.PingAsync(cancellationToken);

        return new ConnectorHealthResult(
            IsHealthy: isHealthy,
            ProviderName: ProviderName,
            ConnectorVersion: ConnectorVersion,
            CheckedAt: checkedAt,
            LatencyMs: latencyMs,
            Message: isHealthy
                ? $"AWIN API is reachable. Publisher ID {_options.PublisherId} verified."
                : $"AWIN health check failed: {errorMessage}");
    }
}
