using SaverSearch.Application.Common.Models;
using SaverSearch.Application.Dtos;

namespace SaverSearch.Application.Common.Interfaces;

public interface ICategoryService
{
    Task<CategoryDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<CategoryDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PaginatedList<CategoryDto>> GetPagedAsync(CategoryQueryParameters query, CancellationToken cancellationToken = default);
    Task<CategoryDto> CreateAsync(CreateCategoryDto dto, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Guid id, UpdateCategoryDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IRetailerService
{
    Task<RetailerDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<RetailerDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PaginatedList<RetailerDto>> GetPagedAsync(RetailerQueryParameters query, CancellationToken cancellationToken = default);
    Task<RetailerDto> CreateAsync(CreateRetailerDto dto, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Guid id, UpdateRetailerDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IProviderService
{
    Task<ProviderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProviderDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PaginatedList<ProviderDto>> GetPagedAsync(ProviderQueryParameters query, CancellationToken cancellationToken = default);
    Task<ProviderDto> CreateAsync(CreateProviderDto dto, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Guid id, UpdateProviderDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IOfferTypeService
{
    Task<OfferTypeDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<OfferTypeDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PaginatedList<OfferTypeDto>> GetPagedAsync(OfferTypeQueryParameters query, CancellationToken cancellationToken = default);
    Task<OfferTypeDto> CreateAsync(CreateOfferTypeDto dto, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Guid id, UpdateOfferTypeDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IOfferService
{
    Task<OfferDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<OfferDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PaginatedList<OfferDto>> GetPagedAsync(OfferQueryParameters query, CancellationToken cancellationToken = default);
    Task<OfferDto> CreateAsync(CreateOfferDto dto, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Guid id, UpdateOfferDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
