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
            services.Configure<SaverSearch.Application.Common.Models.Pipeline.Normalisation.NormalisationSettings>(configuration.GetSection("NormalisationSettings"));
        }
        else
        {
            services.Configure<ConfidenceSettings>(_ => {});
            services.Configure<SaverSearch.Application.Common.Models.Pipeline.Normalisation.NormalisationSettings>(_ => {});
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
        services.AddScoped<IOfferResolver, SaverSearch.Application.Services.Pipeline.OfferResolver>();

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

        // Register Rules Engine & Evaluators
        services.AddScoped<IRulesEngine, SaverSearch.Application.Services.Pipeline.RulesEngine>();

        var ruleTypes = assembly.GetTypes()
            .Where(t => typeof(IRuleEvaluator).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var type in ruleTypes)
        {
            services.AddScoped(typeof(IRuleEvaluator), type);
        }

        // Register Savings Calculator & Strategies
        services.AddScoped<ISavingsCalculator, SaverSearch.Application.Services.Pipeline.SavingsCalculator>();

        var calcStrategyTypes = assembly.GetTypes()
            .Where(t => typeof(ISavingsCalculationStrategy).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var type in calcStrategyTypes)
        {
            services.AddScoped(typeof(ISavingsCalculationStrategy), type);
        }

        // Register Offer Normalisation Engine & Strategies
        services.AddScoped<IOfferNormalisationEngine, SaverSearch.Application.Services.Pipeline.OfferNormalisationEngine>();

        var normStrategyTypes = assembly.GetTypes()
            .Where(t => typeof(INormalisationStrategy).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var type in normStrategyTypes)
        {
            services.AddScoped(typeof(INormalisationStrategy), type);
        }

        return services;
    }
}
