using Microsoft.EntityFrameworkCore;
using SaverSearch.Domain.Entities;

namespace SaverSearch.Application.Common.Interfaces;

public interface ISaverSearchDbContext
{
    DbSet<Category> Categories { get; }
    DbSet<Retailer> Retailers { get; }
    DbSet<Provider> Providers { get; }
    DbSet<OfferType> OfferTypes { get; }
    DbSet<Offer> Offers { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
