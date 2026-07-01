namespace SaverSearch.Application.Dtos;

public record OfferTypeDto(Guid Id, string Name, string? Description);

public record CreateOfferTypeDto(string Name, string? Description);

public record UpdateOfferTypeDto(string Name, string? Description);
