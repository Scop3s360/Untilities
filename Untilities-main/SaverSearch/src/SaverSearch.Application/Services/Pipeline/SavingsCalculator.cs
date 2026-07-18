using SaverSearch.Application.Common.Interfaces;
using SaverSearch.Application.Common.Models.Pipeline;
using SaverSearch.Application.Common.Models.Pipeline.Calculations;

namespace SaverSearch.Application.Services.Pipeline;

public class SavingsCalculator(IEnumerable<ISavingsCalculationStrategy> strategies) : ISavingsCalculator
{
    private readonly List<ISavingsCalculationStrategy> _strategies = strategies.ToList();

    public async Task<CalculatedOffer> CalculateAsync(ResolvedOffer resolvedOffer, DiscoveryContext context, CancellationToken cancellationToken = default)
    {
        // 1. Locate match strategy
        var strategy = _strategies.FirstOrDefault(s => s.CanCalculate(resolvedOffer.Offer));

        if (strategy != null)
        {
            return await strategy.CalculateAsync(resolvedOffer, context, cancellationToken);
        }

        // 2. Fallback default 0 calculation
        var steps = new List<CalculationStep>
        {
            new CalculationStep("No matching calculation strategy resolved.", 0.0m),
            new CalculationStep("Final Saving: £0.00", 0.0m)
        };

        var diagnostics = new CalculatedOfferDiagnostics(0, "Fallback Strategy", new List<string> { "Fallback calculation used." });

        return new CalculatedOffer(
            resolvedOffer.Offer,
            resolvedOffer.Retailer,
            resolvedOffer.Provider,
            resolvedOffer.OfferType,
            context.TargetSpend,
            0.0m,
            0.0m,
            0.0m,
            null,
            false,
            "Fallback Strategy",
            steps,
            100.0,
            diagnostics
        );
    }
}
