using FluentValidation;
using SaverSearch.Application.Dtos;

namespace SaverSearch.Application.Common.Validators;

public class CreateOfferTypeDtoValidator : AbstractValidator<CreateOfferTypeDto>
{
    public CreateOfferTypeDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("OfferType Name is required.")
            .MaximumLength(100).WithMessage("OfferType Name must not exceed 100 characters.");
    }
}

public class UpdateOfferTypeDtoValidator : AbstractValidator<UpdateOfferTypeDto>
{
    public UpdateOfferTypeDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("OfferType Name is required.")
            .MaximumLength(100).WithMessage("OfferType Name must not exceed 100 characters.");
    }
}
