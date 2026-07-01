using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SaverSearch.Application.Common.Interfaces;
using SaverSearch.Infrastructure.Persistence.Contexts;
using SaverSearch.Infrastructure.Persistence.Repositories;
using SaverSearch.Infrastructure.Providers.Scrapers;
using SaverSearch.Infrastructure.Services.Caching;
using SaverSearch.Infrastructure.Services.Notifications;

namespace SaverSearch.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // EF Core with SQLite (Easily replaceable by replacing UseSqlite with UseSqlServer/UseNpgsql)
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
                               ?? "Data Source=SaverSearch.db";
        
        services.AddDbContext<SaverSearchDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<ISaverSearchDbContext>(provider => 
            provider.GetRequiredService<SaverSearchDbContext>());

        // Register Repositories and Unit of Work
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Register Providers & Services
        services.AddScoped<IOfferScraper, TopCashbackScraper>();
        services.AddSingleton<ICacheService, RedisCacheService>();
        services.AddTransient<INotificationService, EmailNotificationService>();

        return services;
    }
}
