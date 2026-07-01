namespace SaverSearch.Domain.Entities;

public enum ImportJobStatus
{
    Running,
    Completed,
    PartialSuccess,
    Failed
}

public class ImportJobRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string ProviderName { get; set; }
    public required string ConnectorVersion { get; set; }
    public ImportJobStatus Status { get; set; } = ImportJobStatus.Running;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public long DurationMs { get; set; }
    public int OffersDownloaded { get; set; }
    public int OffersValidated { get; set; }
    public int OffersAdded { get; set; }
    public int OffersUpdated { get; set; }
    public int OffersDeactivated { get; set; }
    public int ValidationWarningCount { get; set; }
    public string? ErrorMessage { get; set; }
    public string Warnings { get; set; } = "[]"; // JSON-serialised List<string>
}
