using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SaverSearch.Application.Dtos.Discovery;

/// <summary>User goal intent for the discovery request.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UserGoal
{
    MaximumSavings,
    Balanced,
    LowestRisk,
    LowestComplexity,
    HighestConfidence
}

/// <summary>
/// Request body for POST /api/discover.
/// </summary>
public class DiscoveryRequest
{
    /// <summary>Search query — retailer name, URL, category, or natural language.</summary>
    [Required(ErrorMessage = "Query is required.")]
    [MinLength(2, ErrorMessage = "Query must be at least 2 characters.")]
    [MaxLength(500, ErrorMessage = "Query must not exceed 500 characters.")]
    public string Query { get; set; } = string.Empty;

    /// <summary>Optional intended spend amount. Defaults to £100 when omitted.</summary>
    [Range(0, double.MaxValue, ErrorMessage = "SpendAmount must be zero or greater.")]
    public decimal? SpendAmount { get; set; }

    /// <summary>User intent goal. Defaults to Balanced when omitted.</summary>
    public UserGoal UserGoal { get; set; } = UserGoal.Balanced;

    /// <summary>Optional retailer slug to search directly.</summary>
    public string? RetailerSlug { get; set; }

    /// <summary>Optional user region (ISO 3166-1 alpha-2, e.g. "GB").</summary>
    public string? UserRegion { get; set; }

    /// <summary>Optional payment method hint (e.g. "AmexGold").</summary>
    public string? PaymentMethod { get; set; }

    /// <summary>Optional correlation ID for request tracing. Auto-generated if omitted.</summary>
    public string? CorrelationId { get; set; }
}
