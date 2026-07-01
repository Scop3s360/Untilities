using AutoMapper;
using SaverSearch.Application.Dtos;
using SaverSearch.Domain.Entities;

namespace SaverSearch.Application.Common.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Category
        CreateMap<Category, CategoryDto>();
        CreateMap<CreateCategoryDto, Category>();
        CreateMap<UpdateCategoryDto, Category>();

        // Retailer
        CreateMap<Retailer, RetailerDto>();
        CreateMap<CreateRetailerDto, Retailer>();
        CreateMap<UpdateRetailerDto, Retailer>();

        // Provider
        CreateMap<Provider, ProviderDto>();
        CreateMap<CreateProviderDto, Provider>();
        CreateMap<UpdateProviderDto, Provider>();

        // OfferType
        CreateMap<OfferType, OfferTypeDto>();
        CreateMap<CreateOfferTypeDto, OfferType>();
        CreateMap<UpdateOfferTypeDto, OfferType>();

        // Offer
        CreateMap<Offer, OfferDto>();
        CreateMap<CreateOfferDto, Offer>();
        CreateMap<UpdateOfferDto, Offer>();
    }
}
