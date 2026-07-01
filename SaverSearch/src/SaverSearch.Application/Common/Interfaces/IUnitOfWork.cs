using SaverSearch.Domain.Entities;

namespace SaverSearch.Application.Common.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IGenericRepository<Category> Categories { get; }
    IGenericRepository<Retailer> Retailers { get; }
    IGenericRepository<Provider> Providers { get; }
    IGenericRepository<OfferType> OfferTypes { get; }
    IGenericRepository<Offer> Offers { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
