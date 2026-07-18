using System.ComponentModel.DataAnnotations;

namespace SaverSearch.Infrastructure.Providers.Connectors.Awin;

/// <summary>
/// Strongly-typed configuration for the AWIN Publisher API connector.
/// Loaded from appsettings.json under the key "Acquisition:Awin".
/// Credentials must be stored in environment variables or user secrets — never in appsettings.json.
/// </summary>
public sealed class AwinConnectorOptions
{
    public const string SectionKey = "Acquisition:Awin";

    /// <summary>AWIN Publisher ID assigned during account registration.</summary>
    public int PublisherId { get; set; }

    /// <summary>Bearer token generated in the AWIN dashboard: Toolbox → API Credentials.</summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>AWIN API base URL. Defaults to https://api.awin.com.</summary>
    public string BaseUrl { get; set; } = "https://api.awin.com";

    /// <summary>Two-letter region code. "GB" for UK.</summary>
    public string RegionCode { get; set; } = "GB";

    /// <summary>Conservative call rate — below AWIN's hard limit of 20/min.</summary>
    [Range(1, 20)]
    public int RateLimitPerMinute { get; set; } = 18;

    /// <summary>Maximum number of retry attempts for transient failures.</summary>
    [Range(0, 5)]
    public int MaxRetries { get; set; } = 3;

    /// <summary>Base delay in milliseconds for the first retry.</summary>
    [Range(100, 30000)]
    public int RetryBaseDelayMs { get; set; } = 2000;

    /// <summary>HTTP request timeout in seconds.</summary>
    [Range(5, 120)]
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// When true, the connector is active and will be discovered by the acquisition engine.
    /// Set to false to disable without removing the configuration.
    /// </summary>
    public bool Enabled { get; set; } = false; // Default: off until credentials are configured

    /// <summary>Validates that required credentials are present.</summary>
    public bool IsConfigured =>
        PublisherId > 0 && !string.IsNullOrWhiteSpace(AccessToken);
}
