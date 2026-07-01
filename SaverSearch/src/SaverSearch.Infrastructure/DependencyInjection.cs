using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SaverSearch.Application.Common.Interfaces;
using SaverSearch.Application.Common.Interfaces.Acquisition;
using SaverSearch.Infrastructure.Persistence.Contexts;
using SaverSearch.Infrastructure.Persistence.Repositories;
using SaverSearch.Infrastructure.Providers.Connectors.Awin;
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

        // ── AWIN Connector ────────────────────────────────────────────────────
        // Register AWIN options (credentials must be set in user secrets or environment variables)
        services.AddOptions<AwinConnectorOptions>()
            .Bind(configuration.GetSection(AwinConnectorOptions.SectionKey));

        // Named HttpClient for AWIN — timeout and User-Agent configured here.
        // AwinApiClient resolves this via IHttpClientFactory.CreateClient("AWIN").
        services.AddHttpClient("AWIN", (sp, client) =>
        {
            var opts = configuration
                .GetSection(AwinConnectorOptions.SectionKey)
                .Get<AwinConnectorOptions>() ?? new AwinConnectorOptions();

            client.BaseAddress = new Uri(opts.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("SaverSearch/1.0 (+https://saversearch.co.uk)");
        });

        services.AddScoped<AwinApiClient>();

        // ── Auto-discover all IProviderConnector implementations in this assembly ──
        // AWIN connector is discovered here automatically.
        // To add a new connector: implement IProviderConnector and place it in this assembly.
        var assembly = Assembly.GetExecutingAssembly();
        var connectorTypes = assembly.GetTypes()
            .Where(t => typeof(IProviderConnector).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var type in connectorTypes)
            services.AddScoped(typeof(IProviderConnector), type);

        return services;
    }
}
