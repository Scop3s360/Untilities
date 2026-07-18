using FluentValidation;
using SaverSearch.Application.Dtos;

namespace SaverSearch.Application.Common.Validators;

public class CreateProviderDtoValidator : AbstractValidator<CreateProviderDto>
{
    public CreateProviderDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Provider Name is required.")
            .MaximumLength(100).WithMessage("Provider Name must not exceed 100 characters.");

        RuleFor(x => x.Website)
            .NotEmpty().WithMessage("Website URL is required.");
    }
}

public class UpdateProviderDtoValidator : AbstractValidator<UpdateProviderDto>
{
    public UpdateProviderDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Provider Name is required.")
            .MaximumLength(100).WithMessage("Provider Name must not exceed 100 characters.");

        RuleFor(x => x.Website)
            .NotEmpty().WithMessage("Website URL is required.");
    }
}
