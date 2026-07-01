using FluentValidation;
using SaverSearch.Application.Dtos;

namespace SaverSearch.Application.Common.Validators;

public class CreateRetailerDtoValidator : AbstractValidator<CreateRetailerDto>
{
    public CreateRetailerDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Retailer Name is required.")
            .MaximumLength(150).WithMessage("Retailer Name must not exceed 150 characters.");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Retailer Slug is required.")
            .MaximumLength(150).WithMessage("Retailer Slug must not exceed 150 characters.");

        RuleFor(x => x.Website)
            .NotEmpty().WithMessage("Website URL is required.")
            .Must(LinkMustBeAValidLink).WithMessage("Website must be a valid URL.");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("CategoryId is required.");
    }

    private bool LinkMustBeAValidLink(string link)
    {
        if (string.IsNullOrWhiteSpace(link))
            return false;

        return Uri.TryCreate(link, UriKind.Absolute, out var outUri) 
               && (outUri.Scheme == Uri.UriSchemeHttp || outUri.Scheme == Uri.UriSchemeHttps);
    }
}

public class UpdateRetailerDtoValidator : AbstractValidator<UpdateRetailerDto>
{
    public UpdateRetailerDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Retailer Name is required.")
            .MaximumLength(150).WithMessage("Retailer Name must not exceed 150 characters.");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Retailer Slug is required.")
            .MaximumLength(150).WithMessage("Retailer Slug must not exceed 150 characters.");

        RuleFor(x => x.Website)
            .NotEmpty().WithMessage("Website URL is required.")
            .Must(LinkMustBeAValidLink).WithMessage("Website must be a valid URL.");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("CategoryId is required.");
    }

    private bool LinkMustBeAValidLink(string link)
    {
        if (string.IsNullOrWhiteSpace(link))
            return false;

        return Uri.TryCreate(link, UriKind.Absolute, out var outUri) 
               && (outUri.Scheme == Uri.UriSchemeHttp || outUri.Scheme == Uri.UriSchemeHttps);
    }
}
