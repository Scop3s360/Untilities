using FluentValidation;
using SaverSearch.Application.Dtos;

namespace SaverSearch.Application.Common.Validators;

public class CreateOfferDtoValidator : AbstractValidator<CreateOfferDto>
{
    public CreateOfferDtoValidator()
    {
        RuleFor(x => x.RetailerId)
            .NotEmpty().WithMessage("Retailer is required.");

        RuleFor(x => x.ProviderId)
            .NotEmpty().WithMessage("Provider is required.");

        RuleFor(x => x.OfferTypeId)
            .NotEmpty().WithMessage("OfferType is required.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Offer Title is required.")
            .MaximumLength(200).WithMessage("Offer Title must not exceed 200 characters.");

        RuleFor(x => x.OfferUrl)
            .NotEmpty().WithMessage("Offer URL is required.")
            .MaximumLength(1000).WithMessage("Offer URL must not exceed 1000 characters.");

        RuleFor(x => x.Value)
            .GreaterThan(0).WithMessage("Value must be greater than zero.");

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
            .WithMessage("EndDate cannot be before StartDate.");
    }
}

public class UpdateOfferDtoValidator : AbstractValidator<UpdateOfferDto>
{
    public UpdateOfferDtoValidator()
    {
        RuleFor(x => x.RetailerId)
            .NotEmpty().WithMessage("Retailer is required.");

        RuleFor(x => x.ProviderId)
            .NotEmpty().WithMessage("Provider is required.");

        RuleFor(x => x.OfferTypeId)
            .NotEmpty().WithMessage("OfferType is required.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Offer Title is required.")
            .MaximumLength(200).WithMessage("Offer Title must not exceed 200 characters.");

        RuleFor(x => x.OfferUrl)
            .NotEmpty().WithMessage("Offer URL is required.")
            .MaximumLength(1000).WithMessage("Offer URL must not exceed 1000 characters.");

        RuleFor(x => x.Value)
            .GreaterThan(0).WithMessage("Value must be greater than zero.");

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
            .WithMessage("EndDate cannot be before StartDate.");
    }
}
