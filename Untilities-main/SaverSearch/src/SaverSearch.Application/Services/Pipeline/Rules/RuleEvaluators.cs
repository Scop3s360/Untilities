using System.Diagnostics;
using SaverSearch.Application.Common.Interfaces;
using SaverSearch.Application.Common.Models.Pipeline;
using SaverSearch.Application.Common.Models.Pipeline.Rules;

namespace SaverSearch.Application.Services.Pipeline.Rules;

public class MinimumSpendRule : IRuleEvaluator
{
    public string RuleName => "Minimum Spend Rule";
    public string Category => "Financial";

    public Task<RuleResult> EvaluateAsync(ResolvedOffer resolvedOffer, DiscoveryContext context, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var offer = resolvedOffer.Offer;
        var minSpend = offer.MinimumSpend ?? 0.0m;
        
        var passed = context.TargetSpend >= minSpend;
        stopwatch.Stop();

        var metadata = new Dictionary<string, string>
        {
            { "TargetSpend", context.TargetSpend.ToString("C") },
            { "RequiredMinimumSpend", minSpend.ToString("C") }
        };

        return Task.FromResult(new RuleResult(
            RuleName,
            Category,
            passed,
            passed ? "Info" : "Critical",
            passed ? null : $"Target spend ({context.TargetSpend:C}) is less than minimum required ({minSpend:C}).",
            passed ? $"Passed: Target spend meets the minimum required spend of {minSpend:C}." : $"Failed: Target spend must be at least {minSpend:C}.",
            metadata,
            stopwatch.ElapsedMilliseconds
        ));
    }
}

public class OfferDateRule : IRuleEvaluator
{
    public string RuleName => "Offer Date Range Rule";
    public string Category => "Time";

    public Task<RuleResult> EvaluateAsync(ResolvedOffer resolvedOffer, DiscoveryContext context, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var offer = resolvedOffer.Offer;
        var now = DateTime.UtcNow;
        var passed = true;
        string? reason = null;

        if (offer.StartDate.HasValue && offer.StartDate.Value > now)
        {
            passed = false;
            reason = $"Offer is not yet active. Starts at: {offer.StartDate.Value}.";
        }
        else if (offer.EndDate.HasValue && offer.EndDate.Value < now)
        {
            passed = false;
            reason = $"Offer has expired. Expired at: {offer.EndDate.Value}.";
        }

        stopwatch.Stop();

        var metadata = new Dictionary<string, string>
        {
            { "EvaluatedAt", now.ToString() },
            { "StartDate", offer.StartDate?.ToString() ?? "None" },
            { "EndDate", offer.EndDate?.ToString() ?? "None" }
        };

        return Task.FromResult(new RuleResult(
            RuleName,
            Category,
            passed,
            "Critical",
            reason,
            passed ? "Passed: Offer is within valid date ranges." : $"Failed: {reason}",
            metadata,
            stopwatch.ElapsedMilliseconds
        ));
    }
}

public class PaymentMethodRule : IRuleEvaluator
{
    public string RuleName => "Payment Method Constraint Rule";
    public string Category => "Eligibility";

    public Task<RuleResult> EvaluateAsync(ResolvedOffer resolvedOffer, DiscoveryContext context, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var offer = resolvedOffer.Offer;
        var passed = true;
        string? reason = null;
        var severity = "Info";

        // Check if offer title or terms specify a payment method (e.g. Barclaycard, Amex)
        var terms = (offer.Terms ?? string.Empty).ToLower();
        var title = (offer.Title ?? string.Empty).ToLower();
        var userCard = (context.PaymentMethod ?? string.Empty).ToLower();

        if ((terms.Contains("barclaycard") || title.Contains("barclaycard")) && !userCard.Contains("barclaycard"))
        {
            passed = false;
            severity = "Critical";
            reason = "This offer requires payment with a Barclaycard.";
        }
        else if ((terms.Contains("amex") || title.Contains("amex") || terms.Contains("american express")) && !userCard.Contains("amex") && !userCard.Contains("american express"))
        {
            passed = false;
            severity = "Critical";
            reason = "This offer requires payment with an American Express (Amex) card.";
        }

        stopwatch.Stop();

        var metadata = new Dictionary<string, string>
        {
            { "UserPaymentMethod", context.PaymentMethod ?? "None" },
            { "Severity", severity }
        };

        return Task.FromResult(new RuleResult(
            RuleName,
            Category,
            passed,
            severity,
            reason,
            passed ? "Passed: No card constraints violated." : $"Failed: {reason}",
            metadata,
            stopwatch.ElapsedMilliseconds
        ));
    }
}

public class RegionRule : IRuleEvaluator
{
    public string RuleName => "Geographical Region Rule";
    public string Category => "Geography";

    public Task<RuleResult> EvaluateAsync(ResolvedOffer resolvedOffer, DiscoveryContext context, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var offer = resolvedOffer.Offer;
        var passed = true;
        string? reason = null;

        var terms = (offer.Terms ?? string.Empty).ToLower();
        var userRegion = (context.UserRegion ?? string.Empty).ToLower();

        if (terms.Contains("uk only") && !string.IsNullOrEmpty(userRegion) && userRegion != "uk" && userRegion != "united kingdom")
        {
            passed = false;
            reason = $"Offer is restricted to UK residents only. User region: {context.UserRegion}.";
        }

        stopwatch.Stop();

        var metadata = new Dictionary<string, string>
        {
            { "UserRegion", context.UserRegion ?? "None" }
        };

        return Task.FromResult(new RuleResult(
            RuleName,
            Category,
            passed,
            "Critical",
            reason,
            passed ? "Passed: No regional constraints violated." : $"Failed: {reason}",
            metadata,
            stopwatch.ElapsedMilliseconds
        ));
    }
}
