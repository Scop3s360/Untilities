using FluentValidation.Results;
using SaverSearch.Application.Common.Validators;
using SaverSearch.Application.Dtos;
using SaverSearch.Domain.Entities;
using Xunit;

namespace SaverSearch.Tests.UnitTests;

public class ValidatorTests
{
    [Fact]
    public void CreateCategoryDtoValidator_ShouldFail_WhenNameIsEmpty()
    {
        var validator = new CreateCategoryDtoValidator();
        var dto = new CreateCategoryDto(Name: "", Description: "Test");
        
        var result = validator.Validate(dto);
        
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Fact]
    public void CreateRetailerDtoValidator_ShouldFail_WhenWebsiteIsInvalidUrl()
    {
        var validator = new CreateRetailerDtoValidator();
        var dto = new CreateRetailerDto(
            Name: "Amazon",
            Slug: "amazon",
            Website: "invalid-url",
            LogoUrl: null,
            CategoryId: Guid.NewGuid()
        );
        
        var result = validator.Validate(dto);
        
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Website");
    }

    [Fact]
    public void CreateOfferDtoValidator_ShouldFail_WhenValueIsZeroOrNegative()
    {
        var validator = new CreateOfferDtoValidator();
        var dto = new CreateOfferDto(
            RetailerId: Guid.NewGuid(),
            ProviderId: Guid.NewGuid(),
            OfferTypeId: Guid.NewGuid(),
            Title: "Cashback offer",
            Description: null,
            Value: 0.0m,
            ValueType: OfferValueType.Percentage,
            MinimumSpend: null,
            MaximumReward: null,
            StartDate: null,
            EndDate: null,
            Terms: null,
            OfferUrl: "https://amazon.co.uk"
        );
        
        var result = validator.Validate(dto);
        
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Value");
    }

    [Fact]
    public void CreateOfferDtoValidator_ShouldFail_WhenEndDateIsBeforeStartDate()
    {
        var validator = new CreateOfferDtoValidator();
        var dto = new CreateOfferDto(
            RetailerId: Guid.NewGuid(),
            ProviderId: Guid.NewGuid(),
            OfferTypeId: Guid.NewGuid(),
            Title: "Cashback offer",
            Description: null,
            Value: 5.0m,
            ValueType: OfferValueType.Percentage,
            MinimumSpend: null,
            MaximumReward: null,
            StartDate: DateTime.UtcNow.AddDays(5),
            EndDate: DateTime.UtcNow.AddDays(2),
            Terms: null,
            OfferUrl: "https://amazon.co.uk"
        );
        
        var result = validator.Validate(dto);
        
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("EndDate cannot be before StartDate"));
    }
}
