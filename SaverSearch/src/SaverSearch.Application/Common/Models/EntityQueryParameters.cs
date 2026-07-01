namespace SaverSearch.Application.Common.Models;

public record CategoryQueryParameters : QueryParameters
{
    public string? Name { get; set; }
    public bool? IsActive { get; set; }
}

public record RetailerQueryParameters : QueryParameters
{
    public Guid? CategoryId { get; set; }
    public bool? IsActive { get; set; }
}

public record ProviderQueryParameters : QueryParameters
{
    public bool? IsActive { get; set; }
}

public record OfferTypeQueryParameters : QueryParameters
{
    public string? Name { get; set; }
}

public record OfferQueryParameters : QueryParameters
{
    public Guid? ProviderId { get; set; }
    public Guid? RetailerId { get; set; }
    public Guid? OfferTypeId { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
