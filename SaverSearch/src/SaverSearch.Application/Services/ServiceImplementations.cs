using AutoMapper;
using SaverSearch.Application.Common.Extensions;
using SaverSearch.Application.Common.Interfaces;
using SaverSearch.Application.Common.Models;
using SaverSearch.Application.Dtos;
using SaverSearch.Domain.Entities;

namespace SaverSearch.Application.Services;

public class CategoryService(IUnitOfWork unitOfWork, IMapper mapper) : ICategoryService
{
    public async Task<CategoryDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await unitOfWork.Categories.GetByIdAsync(id, cancellationToken);
        return mapper.Map<CategoryDto>(category);
    }

    public async Task<IEnumerable<CategoryDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var categories = await unitOfWork.Categories.GetAllAsync(cancellationToken);
        return mapper.Map<IEnumerable<CategoryDto>>(categories);
    }

    public async Task<PaginatedList<CategoryDto>> GetPagedAsync(CategoryQueryParameters query, CancellationToken cancellationToken = default)
    {
        var dbQuery = unitOfWork.Categories.GetQueryable(asNoTracking: true);

        // Apply filters
        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            dbQuery = dbQuery.Where(c => c.Name.ToLower().Contains(query.Name.ToLower()));
        }

        if (query.IsActive.HasValue)
        {
            dbQuery = dbQuery.Where(c => c.IsActive == query.IsActive.Value);
        }

        // Apply Sorting and Pagination
        dbQuery = dbQuery.ApplySorting(query.SortBy, query.SortOrder);
        var pagedEntities = await dbQuery.ToPaginatedListAsync(query.PageNumber, query.PageSize, cancellationToken);

        // Map items to DTOs
        var mappedItems = mapper.Map<IEnumerable<CategoryDto>>(pagedEntities.Items);
        return new PaginatedList<CategoryDto>(mappedItems, pagedEntities.TotalCount, pagedEntities.CurrentPage, pagedEntities.PageSize);
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryDto dto, CancellationToken cancellationToken = default)
    {
        var category = mapper.Map<Category>(dto);
        await unitOfWork.Categories.AddAsync(category, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return mapper.Map<CategoryDto>(category);
    }

    public async Task<bool> UpdateAsync(Guid id, UpdateCategoryDto dto, CancellationToken cancellationToken = default)
    {
        var category = await unitOfWork.Categories.GetByIdAsync(id, cancellationToken);
        if (category == null) return false;

        mapper.Map(dto, category);
        unitOfWork.Categories.Update(category);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await unitOfWork.Categories.GetByIdAsync(id, cancellationToken);
        if (category == null) return false;

        unitOfWork.Categories.Delete(category);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public class RetailerService(IUnitOfWork unitOfWork, IMapper mapper) : IRetailerService
{
    public async Task<RetailerDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var retailer = await unitOfWork.Retailers.GetByIdAsync(id, cancellationToken);
        return mapper.Map<RetailerDto>(retailer);
    }

    public async Task<IEnumerable<RetailerDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var retailers = await unitOfWork.Retailers.GetAllAsync(cancellationToken);
        return mapper.Map<IEnumerable<RetailerDto>>(retailers);
    }

    public async Task<PaginatedList<RetailerDto>> GetPagedAsync(RetailerQueryParameters query, CancellationToken cancellationToken = default)
    {
        var dbQuery = unitOfWork.Retailers.GetQueryable(asNoTracking: true);

        // Apply filters
        if (query.CategoryId.HasValue)
        {
            dbQuery = dbQuery.Where(r => r.CategoryId == query.CategoryId.Value);
        }

        if (query.IsActive.HasValue)
        {
            dbQuery = dbQuery.Where(r => r.IsActive == query.IsActive.Value);
        }

        // Apply Sorting and Pagination
        dbQuery = dbQuery.ApplySorting(query.SortBy, query.SortOrder);
        var pagedEntities = await dbQuery.ToPaginatedListAsync(query.PageNumber, query.PageSize, cancellationToken);

        var mappedItems = mapper.Map<IEnumerable<RetailerDto>>(pagedEntities.Items);
        return new PaginatedList<RetailerDto>(mappedItems, pagedEntities.TotalCount, pagedEntities.CurrentPage, pagedEntities.PageSize);
    }

    public async Task<RetailerDto> CreateAsync(CreateRetailerDto dto, CancellationToken cancellationToken = default)
    {
        var retailer = mapper.Map<Retailer>(dto);
        retailer.CreatedDate = DateTime.UtcNow;
        retailer.UpdatedDate = DateTime.UtcNow;
        
        await unitOfWork.Retailers.AddAsync(retailer, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return mapper.Map<RetailerDto>(retailer);
    }

    public async Task<bool> UpdateAsync(Guid id, UpdateRetailerDto dto, CancellationToken cancellationToken = default)
    {
        var retailer = await unitOfWork.Retailers.GetByIdAsync(id, cancellationToken);
        if (retailer == null) return false;

        mapper.Map(dto, retailer);
        retailer.UpdatedDate = DateTime.UtcNow;
        
        unitOfWork.Retailers.Update(retailer);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var retailer = await unitOfWork.Retailers.GetByIdAsync(id, cancellationToken);
        if (retailer == null) return false;

        unitOfWork.Retailers.Delete(retailer);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public class ProviderService(IUnitOfWork unitOfWork, IMapper mapper) : IProviderService
{
    public async Task<ProviderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var provider = await unitOfWork.Providers.GetByIdAsync(id, cancellationToken);
        return mapper.Map<ProviderDto>(provider);
    }

    public async Task<IEnumerable<ProviderDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var providers = await unitOfWork.Providers.GetAllAsync(cancellationToken);
        return mapper.Map<IEnumerable<ProviderDto>>(providers);
    }

    public async Task<PaginatedList<ProviderDto>> GetPagedAsync(ProviderQueryParameters query, CancellationToken cancellationToken = default)
    {
        var dbQuery = unitOfWork.Providers.GetQueryable(asNoTracking: true);

        // Apply filters
        if (query.IsActive.HasValue)
        {
            dbQuery = dbQuery.Where(p => p.IsActive == query.IsActive.Value);
        }

        // Apply Sorting and Pagination
        dbQuery = dbQuery.ApplySorting(query.SortBy, query.SortOrder);
        var pagedEntities = await dbQuery.ToPaginatedListAsync(query.PageNumber, query.PageSize, cancellationToken);

        var mappedItems = mapper.Map<IEnumerable<ProviderDto>>(pagedEntities.Items);
        return new PaginatedList<ProviderDto>(mappedItems, pagedEntities.TotalCount, pagedEntities.CurrentPage, pagedEntities.PageSize);
    }

    public async Task<ProviderDto> CreateAsync(CreateProviderDto dto, CancellationToken cancellationToken = default)
    {
        var provider = mapper.Map<Provider>(dto);
        provider.CreatedDate = DateTime.UtcNow;
        provider.UpdatedDate = DateTime.UtcNow;

        await unitOfWork.Providers.AddAsync(provider, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return mapper.Map<ProviderDto>(provider);
    }

    public async Task<bool> UpdateAsync(Guid id, UpdateProviderDto dto, CancellationToken cancellationToken = default)
    {
        var provider = await unitOfWork.Providers.GetByIdAsync(id, cancellationToken);
        if (provider == null) return false;

        mapper.Map(dto, provider);
        provider.UpdatedDate = DateTime.UtcNow;

        unitOfWork.Providers.Update(provider);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var provider = await unitOfWork.Providers.GetByIdAsync(id, cancellationToken);
        if (provider == null) return false;

        unitOfWork.Providers.Delete(provider);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public class OfferTypeService(IUnitOfWork unitOfWork, IMapper mapper) : IOfferTypeService
{
    public async Task<OfferTypeDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var offerType = await unitOfWork.OfferTypes.GetByIdAsync(id, cancellationToken);
        return mapper.Map<OfferTypeDto>(offerType);
    }

    public async Task<IEnumerable<OfferTypeDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var offerTypes = await unitOfWork.OfferTypes.GetAllAsync(cancellationToken);
        return mapper.Map<IEnumerable<OfferTypeDto>>(offerTypes);
    }

    public async Task<PaginatedList<OfferTypeDto>> GetPagedAsync(OfferTypeQueryParameters query, CancellationToken cancellationToken = default)
    {
        var dbQuery = unitOfWork.OfferTypes.GetQueryable(asNoTracking: true);

        // Apply filters
        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            dbQuery = dbQuery.Where(ot => ot.Name.ToLower().Contains(query.Name.ToLower()));
        }

        // Apply Sorting and Pagination
        dbQuery = dbQuery.ApplySorting(query.SortBy, query.SortOrder);
        var pagedEntities = await dbQuery.ToPaginatedListAsync(query.PageNumber, query.PageSize, cancellationToken);

        var mappedItems = mapper.Map<IEnumerable<OfferTypeDto>>(pagedEntities.Items);
        return new PaginatedList<OfferTypeDto>(mappedItems, pagedEntities.TotalCount, pagedEntities.CurrentPage, pagedEntities.PageSize);
    }

    public async Task<OfferTypeDto> CreateAsync(CreateOfferTypeDto dto, CancellationToken cancellationToken = default)
    {
        var offerType = mapper.Map<OfferType>(dto);
        await unitOfWork.OfferTypes.AddAsync(offerType, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return mapper.Map<OfferTypeDto>(offerType);
    }

    public async Task<bool> UpdateAsync(Guid id, UpdateOfferTypeDto dto, CancellationToken cancellationToken = default)
    {
        var offerType = await unitOfWork.OfferTypes.GetByIdAsync(id, cancellationToken);
        if (offerType == null) return false;

        mapper.Map(dto, offerType);
        unitOfWork.OfferTypes.Update(offerType);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var offerType = await unitOfWork.OfferTypes.GetByIdAsync(id, cancellationToken);
        if (offerType == null) return false;

        unitOfWork.OfferTypes.Delete(offerType);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public class OfferService(IUnitOfWork unitOfWork, IMapper mapper) : IOfferService
{
    public async Task<OfferDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var offer = await unitOfWork.Offers.GetByIdAsync(id, cancellationToken);
        return mapper.Map<OfferDto>(offer);
    }

    public async Task<IEnumerable<OfferDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var offers = await unitOfWork.Offers.GetAllAsync(cancellationToken);
        return mapper.Map<IEnumerable<OfferDto>>(offers);
    }

    public async Task<PaginatedList<OfferDto>> GetPagedAsync(OfferQueryParameters query, CancellationToken cancellationToken = default)
    {
        var dbQuery = unitOfWork.Offers.GetQueryable(asNoTracking: true);

        // Apply filters
        if (query.ProviderId.HasValue)
        {
            dbQuery = dbQuery.Where(o => o.ProviderId == query.ProviderId.Value);
        }

        if (query.RetailerId.HasValue)
        {
            dbQuery = dbQuery.Where(o => o.RetailerId == query.RetailerId.Value);
        }

        if (query.OfferTypeId.HasValue)
        {
            dbQuery = dbQuery.Where(o => o.OfferTypeId == query.OfferTypeId.Value);
        }

        if (query.IsActive.HasValue)
        {
            dbQuery = dbQuery.Where(o => o.IsActive == query.IsActive.Value);
        }

        if (query.StartDate.HasValue)
        {
            dbQuery = dbQuery.Where(o => o.StartDate >= query.StartDate.Value);
        }

        if (query.EndDate.HasValue)
        {
            dbQuery = dbQuery.Where(o => o.EndDate <= query.EndDate.Value);
        }

        // Apply Sorting and Pagination
        dbQuery = dbQuery.ApplySorting(query.SortBy, query.SortOrder);
        var pagedEntities = await dbQuery.ToPaginatedListAsync(query.PageNumber, query.PageSize, cancellationToken);

        var mappedItems = mapper.Map<IEnumerable<OfferDto>>(pagedEntities.Items);
        return new PaginatedList<OfferDto>(mappedItems, pagedEntities.TotalCount, pagedEntities.CurrentPage, pagedEntities.PageSize);
    }

    public async Task<OfferDto> CreateAsync(CreateOfferDto dto, CancellationToken cancellationToken = default)
    {
        var offer = mapper.Map<Offer>(dto);
        offer.LastUpdated = DateTime.UtcNow;

        await unitOfWork.Offers.AddAsync(offer, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return mapper.Map<OfferDto>(offer);
    }

    public async Task<bool> UpdateAsync(Guid id, UpdateOfferDto dto, CancellationToken cancellationToken = default)
    {
        var offer = await unitOfWork.Offers.GetByIdAsync(id, cancellationToken);
        if (offer == null) return false;

        mapper.Map(dto, offer);
        offer.LastUpdated = DateTime.UtcNow;

        unitOfWork.Offers.Update(offer);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var offer = await unitOfWork.Offers.GetByIdAsync(id, cancellationToken);
        if (offer == null) return false;

        unitOfWork.Offers.Delete(offer);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
