using AutoMapper;
using SaverSearch.Application.Common.Interfaces;
using SaverSearch.Application.Common.Mappings;
using SaverSearch.Application.Dtos;
using SaverSearch.Application.Services;
using SaverSearch.Domain.Entities;
using Xunit;

namespace SaverSearch.Tests.UnitTests;

public class ServiceTests
{
    private readonly IMapper _mapper;
    private readonly MockUnitOfWork _uow;

    public ServiceTests()
    {
        var config = new MapperConfiguration(
            cfg => cfg.AddProfile<MappingProfile>(),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance
        );
        _mapper = config.CreateMapper();
        _uow = new MockUnitOfWork();
    }

    [Fact]
    public async Task CategoryService_CreateAsync_ShouldAddCategoryAndSave()
    {
        // Arrange
        var service = new CategoryService(_uow, _mapper);
        var dto = new CreateCategoryDto("Electronics", "Description");

        // Act
        var result = await service.CreateAsync(dto);

        // Assert
        Assert.Equal("Electronics", result.Name);
        Assert.True(_uow.SaveCalled);
        Assert.Contains(_uow.MockCategories.List, c => c.Name == "Electronics");
    }

    // Mock classes for unit testing service orchestration without database dependencies
    private class MockUnitOfWork : IUnitOfWork
    {
        public bool SaveCalled { get; private set; }
        public MockRepository<Category> MockCategories { get; } = new();
        public MockRepository<Retailer> MockRetailers { get; } = new();
        public MockRepository<Provider> MockProviders { get; } = new();
        public MockRepository<OfferType> MockOfferTypes { get; } = new();
        public MockRepository<Offer> MockOffers { get; } = new();

        public IGenericRepository<Category> Categories => MockCategories;
        public IGenericRepository<Retailer> Retailers => MockRetailers;
        public IGenericRepository<Provider> Providers => MockProviders;
        public IGenericRepository<OfferType> OfferTypes => MockOfferTypes;
        public IGenericRepository<Offer> Offers => MockOffers;

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCalled = true;
            return Task.FromResult(1);
        }

        public void Dispose() { }
    }

    private class MockRepository<T> : IGenericRepository<T> where T : class
    {
        public List<T> List { get; } = new();

        public Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            // Simple mock implementation
            return Task.FromResult<T?>(null);
        }

        public Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IEnumerable<T>>(List);
        }

        public IQueryable<T> GetQueryable(bool asNoTracking = true)
        {
            return List.AsQueryable();
        }

        public Task AddAsync(T entity, CancellationToken cancellationToken = default)
        {
            List.Add(entity);
            return Task.CompletedTask;
        }

        public void Update(T entity) { }
        public void Delete(T entity) { }
    }
}
