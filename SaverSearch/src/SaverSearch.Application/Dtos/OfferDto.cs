namespace SaverSearch.Application.Dtos;

public class OfferDto
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public required string DealUrl { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public required string ProviderName { get; set; }
    public required string OfferType { get; set; }
    public decimal Value { get; set; }
    public string RetailerName { get; set; } = null!;
}
