namespace SaverSearch.Application.Dtos;

public record ProviderDto(
    Guid Id,
    string Name,
    string Website,
    string? LogoUrl,
    string? Description,
    bool IsActive,
    DateTime CreatedDate,
    DateTime UpdatedDate
);

public record CreateProviderDto(
    string Name,
    string Website,
    string? LogoUrl,
    string? Description,
    bool IsActive = true
);

public record UpdateProviderDto(
    string Name,
    string Website,
    string? LogoUrl,
    string? Description,
    bool IsActive
);
