namespace SaverSearch.Domain.Entities;

public class Offer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RetailerId { get; set; }
    public Guid ProviderId { get; set; }
    public Guid OfferTypeId { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public decimal Value { get; set; }
    public OfferValueType ValueType { get; set; }
    public decimal? MinimumSpend { get; set; }
    public decimal? MaximumReward { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Terms { get; set; }
    public required string OfferUrl { get; set; }
    public string? ExternalId { get; set; }
    public bool IsExclusive { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Retailer Retailer { get; set; } = null!;
    public Provider Provider { get; set; } = null!;
    public OfferType OfferType { get; set; } = null!;
}
