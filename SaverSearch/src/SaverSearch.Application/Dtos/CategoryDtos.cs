namespace SaverSearch.Application.Dtos;

public record CategoryDto(Guid Id, string Name, string? Description, bool IsActive);

public record CreateCategoryDto(string Name, string? Description, bool IsActive = true);

public record UpdateCategoryDto(string Name, string? Description, bool IsActive);
