using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SaverSearch.Application.Common.Interfaces;
using SaverSearch.Application.Common.Models.Pipeline;
using SaverSearch.Application.Services;

using Microsoft.Extensions.Configuration;
using SaverSearch.Application.Common.Models.Resolver;

namespace SaverSearch.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration? configuration = null)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Register AutoMapper
        services.AddAutoMapper(cfg => cfg.AddMaps(assembly));

        // Configure ConfidenceSettings Options
        if (configuration != null)
        {
            services.Configure<ConfidenceSettings>(configuration.GetSection("ConfidenceSettings"));
        }
        else
        {
            services.Configure<ConfidenceSettings>(_ => {});
        }

        // Register FluentValidation validators
        services.AddValidatorsFromAssembly(assembly);

        // Register Business Services
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IRetailerService, RetailerService>();
        services.AddScoped<IProviderService, ProviderService>();
        services.AddScoped<IOfferTypeService, OfferTypeService>();
        services.AddScoped<IOfferService, OfferService>();

        // Register Offer Discovery Pipeline & Stages
        services.AddScoped<IOfferDiscoveryPipeline, SaverSearch.Application.Services.Pipeline.OfferDiscoveryPipeline>();

        var stageTypes = assembly.GetTypes()
            .Where(t => typeof(IPipelineStage).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var type in stageTypes)
        {
            services.AddScoped(typeof(IPipelineStage), type);
        }

        // Register Retailer Resolver & Strategies
        services.AddScoped<IRetailerResolver, SaverSearch.Application.Services.Resolver.RetailerResolver>();
        
        var strategyTypes = assembly.GetTypes()
            .Where(t => typeof(IRetailerResolverStrategy).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var type in strategyTypes)
        {
            services.AddScoped(typeof(IRetailerResolverStrategy), type);
        }

        return services;
    }
}
