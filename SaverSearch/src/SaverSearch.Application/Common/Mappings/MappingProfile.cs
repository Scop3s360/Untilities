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
        CreateMap<CreateCategoryDto, Category>(MemberList.None);
        CreateMap<UpdateCategoryDto, Category>(MemberList.None);

        // Retailer
        CreateMap<Retailer, RetailerDto>();
        CreateMap<CreateRetailerDto, Retailer>(MemberList.None);
        CreateMap<UpdateRetailerDto, Retailer>(MemberList.None);

        // Provider
        CreateMap<Provider, ProviderDto>();
        CreateMap<CreateProviderDto, Provider>(MemberList.None);
        CreateMap<UpdateProviderDto, Provider>(MemberList.None);

        // OfferType
        CreateMap<OfferType, OfferTypeDto>();
        CreateMap<CreateOfferTypeDto, OfferType>(MemberList.None);
        CreateMap<UpdateOfferTypeDto, OfferType>(MemberList.None);

        // Offer
        CreateMap<Offer, OfferDto>();
        CreateMap<CreateOfferDto, Offer>(MemberList.None);
        CreateMap<UpdateOfferDto, Offer>(MemberList.None);
    }
}
