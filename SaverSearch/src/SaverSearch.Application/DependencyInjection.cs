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
            services.Configure<SaverSearch.Application.Common.Models.Pipeline.Ranking.RankingSettings>(configuration.GetSection("RankingSettings"));
        }
        else
        {
            services.Configure<ConfidenceSettings>(_ => {});
            services.Configure<SaverSearch.Application.Common.Models.Pipeline.Normalisation.NormalisationSettings>(_ => {});
            services.Configure<SaverSearch.Application.Common.Models.Pipeline.Ranking.RankingSettings>(_ => {});
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

        // Register Ranking Engine, Strategies & Factors
        services.AddScoped<IRankingEngine, SaverSearch.Application.Services.Pipeline.RankingEngine>();

        var rankStrategyTypes = assembly.GetTypes()
            .Where(t => typeof(IRankingStrategy).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var type in rankStrategyTypes)
        {
            services.AddScoped(typeof(IRankingStrategy), type);
        }

        var scoringFactorTypes = assembly.GetTypes()
            .Where(t => typeof(IScoringFactor).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var type in scoringFactorTypes)
        {
            services.AddScoped(typeof(IScoringFactor), type);
        }

        // Register Purchase Planning Engine, Strategies & Evaluators
        services.AddScoped<IPurchasePlanningEngine, SaverSearch.Application.Services.Pipeline.PurchasePlanningEngine>();

        var planStrategyTypes = assembly.GetTypes()
            .Where(t => typeof(IPurchasePlanningStrategy).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var type in planStrategyTypes)
        {
            services.AddScoped(typeof(IPurchasePlanningStrategy), type);
        }

        var compatEvaluatorTypes = assembly.GetTypes()
            .Where(t => typeof(ICompatibilityEvaluator).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var type in compatEvaluatorTypes)
        {
            services.AddScoped(typeof(ICompatibilityEvaluator), type);
        }

        // Register Recommendation Engine, Strategies & Evaluators
        services.AddScoped<IRecommendationEngine, SaverSearch.Application.Services.Pipeline.RecommendationEngine>();

        var recStrategyTypes = assembly.GetTypes()
            .Where(t => typeof(IRecommendationStrategy).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var type in recStrategyTypes)
        {
            services.AddScoped(typeof(IRecommendationStrategy), type);
        }

        var riskEvaluatorTypes = assembly.GetTypes()
            .Where(t => typeof(IRiskEvaluator).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var type in riskEvaluatorTypes)
        {
            services.AddScoped(typeof(IRiskEvaluator), type);
        }

        return services;
    }
}
