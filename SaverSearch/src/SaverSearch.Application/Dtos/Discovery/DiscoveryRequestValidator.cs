using FluentValidation;

namespace SaverSearch.Application.Dtos.Discovery;

public class DiscoveryRequestValidator : AbstractValidator<DiscoveryRequest>
{
    public DiscoveryRequestValidator()
    {
        RuleFor(x => x.Query)
            .NotEmpty().WithMessage("Query is required.")
            .MinimumLength(2).WithMessage("Query must be at least 2 characters.")
            .MaximumLength(500).WithMessage("Query must not exceed 500 characters.");

        RuleFor(x => x.SpendAmount)
            .GreaterThanOrEqualTo(0).WithMessage("SpendAmount must be zero or greater.")
            .When(x => x.SpendAmount.HasValue);

        RuleFor(x => x.UserGoal)
            .IsInEnum().WithMessage("UserGoal must be a valid value: MaximumSavings, Balanced, LowestRisk, LowestComplexity, or HighestConfidence.");
    }
}
