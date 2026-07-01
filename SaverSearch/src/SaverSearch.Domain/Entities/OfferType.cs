namespace SaverSearch.Domain.Entities;

public class OfferType
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public string? Description { get; set; }

    // Navigation property
    public ICollection<Offer> Offers { get; set; } = new List<Offer>();
}
