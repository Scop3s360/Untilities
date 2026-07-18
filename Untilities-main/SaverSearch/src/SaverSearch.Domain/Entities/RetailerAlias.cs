namespace SaverSearch.Domain.Entities;

public class RetailerAlias
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RetailerId { get; set; }
    public required string AliasName { get; set; }

    // Navigation properties
    public Retailer Retailer { get; set; } = null!;
}
