namespace SaverSearch.Application.Common.Models.Pipeline;

public record OfferResolverDiagnostics
{
    public long RetrievalDurationMs { get; init; }
    public int OffersExamined { get; init; }
    public int OffersReturned { get; init; }
    public int OffersRejected { get; init; }
    public Dictionary<Guid, string> RejectionReasons { get; init; } = new();
}
