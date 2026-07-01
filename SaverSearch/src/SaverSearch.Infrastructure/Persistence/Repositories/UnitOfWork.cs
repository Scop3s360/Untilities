using SaverSearch.Application.Common.Interfaces;
using SaverSearch.Domain.Entities;
using SaverSearch.Infrastructure.Persistence.Contexts;

namespace SaverSearch.Infrastructure.Persistence.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly SaverSearchDbContext _context;
    
    private IGenericRepository<Category>? _categories;
    private IGenericRepository<Retailer>? _retailers;
    private IGenericRepository<Provider>? _providers;
    private IGenericRepository<OfferType>? _offerTypes;
    private IGenericRepository<Offer>? _offers;

    public UnitOfWork(SaverSearchDbContext context)
    {
        _context = context;
    }

    public IGenericRepository<Category> Categories => 
        _categories ??= new GenericRepository<Category>(_context);

    public IGenericRepository<Retailer> Retailers => 
        _retailers ??= new GenericRepository<Retailer>(_context);

    public IGenericRepository<Provider> Providers => 
        _providers ??= new GenericRepository<Provider>(_context);

    public IGenericRepository<OfferType> OfferTypes => 
        _offerTypes ??= new GenericRepository<OfferType>(_context);

    public IGenericRepository<Offer> Offers => 
        _offers ??= new GenericRepository<Offer>(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
