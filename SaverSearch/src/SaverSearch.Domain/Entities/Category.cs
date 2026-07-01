namespace SaverSearch.Domain.Entities;

public class Category
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation property
    public ICollection<Retailer> Retailers { get; set; } = new List<Retailer>();
}
