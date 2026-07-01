using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SaverSearch.Application.Common.Interfaces;
using SaverSearch.Application.Services;

namespace SaverSearch.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Register AutoMapper
        services.AddAutoMapper(cfg => cfg.AddMaps(assembly));

        // Register FluentValidation validators
        services.AddValidatorsFromAssembly(assembly);

        // Register Business Services
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IRetailerService, RetailerService>();
        services.AddScoped<IProviderService, ProviderService>();
        services.AddScoped<IOfferTypeService, OfferTypeService>();
        services.AddScoped<IOfferService, OfferService>();

        return services;
    }
}
