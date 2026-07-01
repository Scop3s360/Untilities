namespace SaverSearch.Application.Common.Models.Pipeline.Rules;

public record RuleResult(
    string RuleName,
    string Category,
    bool Passed,
    string Severity, // "Critical", "Warning", "Info"
    string? FailureReason,
    string Explanation,
    IDictionary<string, string> Metadata,
    long ExecutionTimeMs
);
