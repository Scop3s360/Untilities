using SaverSearch.Infrastructure.Providers.Connectors.Shared;

namespace SaverSearch.Infrastructure.Providers.Connectors.Awin;

/// <summary>Base exception for all AWIN connector failures.</summary>
public class AwinConnectorException : Exception
{
    public int? StatusCode { get; }

    public AwinConnectorException(string message, int? statusCode = null, Exception? inner = null)
        : base(message, inner)
    {
        StatusCode = statusCode;
    }
}

/// <summary>401 — Bearer token is invalid or revoked. Do not retry.</summary>
public sealed class AwinAuthenticationException : AwinConnectorException, ITerminalConnectorException
{
    public AwinAuthenticationException()
        : base("AWIN authentication failed. The Bearer token is invalid or has been revoked.", 401) { }
}

/// <summary>429 — Rate limit exceeded. Retry after the specified delay.</summary>
public sealed class AwinRateLimitException : AwinConnectorException, IRateLimitException
{
    public int RetryAfterSeconds { get; }

    public AwinRateLimitException(int retryAfterSeconds = 60)
        : base($"AWIN rate limit exceeded. Retry after {retryAfterSeconds} seconds.", 429)
    {
        RetryAfterSeconds = retryAfterSeconds;
    }
}

/// <summary>404 — Publisher ID or resource not found. Do not retry.</summary>
public sealed class AwinNotFoundException : AwinConnectorException, ITerminalConnectorException
{
    public AwinNotFoundException(string resource)
        : base($"AWIN resource not found: {resource}", 404) { }
}

/// <summary>5xx — AWIN infrastructure failure. May retry.</summary>
public sealed class AwinServiceException : AwinConnectorException
{
    public AwinServiceException(int statusCode, string detail)
        : base($"AWIN service error ({statusCode}): {detail}", statusCode) { }
}
