namespace SaverSearch.Domain.Entities;

public class Retailer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public required string Website { get; set; }
    public string? LogoUrl { get; set; }
    public Guid CategoryId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Category Category { get; set; } = null!;
    public ICollection<Offer> Offers { get; set; } = new List<Offer>();
}
