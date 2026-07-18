using FluentValidation;
using SaverSearch.Application.Dtos;

namespace SaverSearch.Application.Common.Validators;

public class CreateCategoryDtoValidator : AbstractValidator<CreateCategoryDto>
{
    public CreateCategoryDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Category Name is required.")
            .MaximumLength(100).WithMessage("Category Name must not exceed 100 characters.");
    }
}

public class UpdateCategoryDtoValidator : AbstractValidator<UpdateCategoryDto>
{
    public UpdateCategoryDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Category Name is required.")
            .MaximumLength(100).WithMessage("Category Name must not exceed 100 characters.");
    }
}
