namespace SaverSearch.Application.Dtos;

public record RetailerDto(
    Guid Id,
    string Name,
    string Slug,
    string Website,
    string? LogoUrl,
    Guid CategoryId,
    bool IsActive,
    DateTime CreatedDate,
    DateTime UpdatedDate
);

public record CreateRetailerDto(
    string Name,
    string Slug,
    string Website,
    string? LogoUrl,
    Guid CategoryId,
    bool IsActive = true
);

public record UpdateRetailerDto(
    string Name,
    string Slug,
    string Website,
    string? LogoUrl,
    Guid CategoryId,
    bool IsActive
);
