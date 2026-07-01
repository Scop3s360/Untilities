namespace SaverSearch.Domain.Entities;

public class Provider
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public required string Website { get; set; }
    public string? LogoUrl { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

    // Navigation property
    public ICollection<Offer> Offers { get; set; } = new List<Offer>();
}
