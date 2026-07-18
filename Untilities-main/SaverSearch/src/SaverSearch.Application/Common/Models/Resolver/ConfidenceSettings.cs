namespace SaverSearch.Application.Common.Models.Resolver;

public class ConfidenceSettings
{
    public double ExactName { get; set; } = 100.0;
    public double Website { get; set; } = 100.0;
    public double Alias { get; set; } = 95.0;
    public double Slug { get; set; } = 95.0;
    public double Normalized { get; set; } = 90.0;
    public double Partial { get; set; } = 75.0;
    public double FuzzyMinScore { get; set; } = 50.0; // Dynamic fuzzy matching base threshold
}
