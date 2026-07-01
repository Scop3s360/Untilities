using Microsoft.EntityFrameworkCore;
using SaverSearch.Application.Common.Interfaces;
using SaverSearch.Domain.Entities;

namespace SaverSearch.Infrastructure.Persistence.Contexts;

public class SaverSearchDbContext : DbContext, ISaverSearchDbContext
{
    public SaverSearchDbContext(DbContextOptions<SaverSearchDbContext> options)
        : base(options)
    {
    }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Retailer> Retailers => Set<Retailer>();
    public DbSet<Provider> Providers => Set<Provider>();
    public DbSet<OfferType> OfferTypes => Set<OfferType>();
    public DbSet<Offer> Offers => Set<Offer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SaverSearchDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
