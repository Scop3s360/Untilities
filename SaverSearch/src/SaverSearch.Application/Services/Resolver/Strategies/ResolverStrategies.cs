using Microsoft.Extensions.Options;
using SaverSearch.Application.Common.Interfaces;
using SaverSearch.Application.Common.Models.Pipeline;
using SaverSearch.Application.Common.Models.Resolver;
using SaverSearch.Domain.Entities;

namespace SaverSearch.Application.Services.Resolver.Strategies;

public class ExactNameMatchStrategy(IOptions<ConfidenceSettings> options) : IRetailerResolverStrategy
{
    private readonly ConfidenceSettings _settings = options.Value;
    public string StrategyName => "Exact Name Match";

    public Task<IEnumerable<ResolverResult>> MatchAsync(DiscoveryContext context, IEnumerable<Retailer> retailers, CancellationToken cancellationToken = default)
    {
        var query = context.RawQuery.Trim();
        var matches = retailers
            .Where(r => r.Name.Equals(query, StringComparison.OrdinalIgnoreCase))
            .Select(r => new ResolverResult(r, _settings.ExactName, StrategyName, $"Exact match found for '{query}'"));

        return Task.FromResult(matches);
    }
}

public class SlugMatchStrategy(IOptions<ConfidenceSettings> options) : IRetailerResolverStrategy
{
    private readonly ConfidenceSettings _settings = options.Value;
    public string StrategyName => "Slug Match";

    public Task<IEnumerable<ResolverResult>> MatchAsync(DiscoveryContext context, IEnumerable<Retailer> retailers, CancellationToken cancellationToken = default)
    {
        var slug = context.RetailerSlug?.Trim() ?? context.RawQuery.Trim().ToLower().Replace(" ", "-");
        var matches = retailers
            .Where(r => r.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase))
            .Select(r => new ResolverResult(r, _settings.Slug, StrategyName, $"Slug match found for '{slug}'"));

        return Task.FromResult(matches);
    }
}

public class WebsiteMatchStrategy(IOptions<ConfidenceSettings> options) : IRetailerResolverStrategy
{
    private readonly ConfidenceSettings _settings = options.Value;
    public string StrategyName => "Website Match";

    public Task<IEnumerable<ResolverResult>> MatchAsync(DiscoveryContext context, IEnumerable<Retailer> retailers, CancellationToken cancellationToken = default)
    {
        var query = context.RawQuery.Trim().ToLower();
        var matches = retailers
            .Where(r => r.Website.ToLower().Contains(query) || query.Contains(r.Website.ToLower()))
            .Select(r => new ResolverResult(r, _settings.Website, StrategyName, $"Website match found on '{r.Website}'"));

        return Task.FromResult(matches);
    }
}

public class AliasMatchStrategy(IOptions<ConfidenceSettings> options) : IRetailerResolverStrategy
{
    private readonly ConfidenceSettings _settings = options.Value;
    public string StrategyName => "Alias Match";

    public Task<IEnumerable<ResolverResult>> MatchAsync(DiscoveryContext context, IEnumerable<Retailer> retailers, CancellationToken cancellationToken = default)
    {
        var query = context.RawQuery.Trim();
        var matches = new List<ResolverResult>();

        foreach (var retailer in retailers)
        {
            if (retailer.Aliases == null) continue;

            var matchingAlias = retailer.Aliases
                .FirstOrDefault(a => a.AliasName.Equals(query, StringComparison.OrdinalIgnoreCase));

            if (matchingAlias != null)
            {
                matches.Add(new ResolverResult(retailer, _settings.Alias, StrategyName, $"Alias match found on '{matchingAlias.AliasName}'"));
            }
        }

        return Task.FromResult<IEnumerable<ResolverResult>>(matches);
    }
}

public class NormalizedTextMatchStrategy(IOptions<ConfidenceSettings> options) : IRetailerResolverStrategy
{
    private readonly ConfidenceSettings _settings = options.Value;
    public string StrategyName => "Normalized Text Match";

    public Task<IEnumerable<ResolverResult>> MatchAsync(DiscoveryContext context, IEnumerable<Retailer> retailers, CancellationToken cancellationToken = default)
    {
        var normalizedQuery = Normalize(context.RawQuery);
        var matches = retailers
            .Where(r => Normalize(r.Name).Equals(normalizedQuery, StringComparison.OrdinalIgnoreCase))
            .Select(r => new ResolverResult(r, _settings.Normalized, StrategyName, $"Normalized name matched '{normalizedQuery}'"));

        return Task.FromResult(matches);
    }

    private static string Normalize(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        // Remove spaces, dots, dashes, and make lowercase
        return input.Replace(" ", "").Replace(".", "").Replace("-", "").ToLower();
    }
}

public class FuzzyMatchStrategy(IOptions<ConfidenceSettings> options) : IRetailerResolverStrategy
{
    private readonly ConfidenceSettings _settings = options.Value;
    public string StrategyName => "Fuzzy Match";

    public Task<IEnumerable<ResolverResult>> MatchAsync(DiscoveryContext context, IEnumerable<Retailer> retailers, CancellationToken cancellationToken = default)
    {
        var query = context.RawQuery.Trim().ToLower();
        var matches = new List<ResolverResult>();

        foreach (var retailer in retailers)
        {
            var name = retailer.Name.ToLower();
            var distance = GetLevenshteinDistance(query, name);
            var maxLen = Math.Max(query.Length, name.Length);
            
            if (maxLen == 0) continue;

            double similarity = 1.0 - ((double)distance / maxLen);
            double score = similarity * 100.0;

            if (score >= _settings.FuzzyMinScore)
            {
                matches.Add(new ResolverResult(retailer, score, StrategyName, $"Fuzzy Levenshtein match with similarity {similarity:P1}"));
            }
        }

        return Task.FromResult<IEnumerable<ResolverResult>>(matches);
    }

    private static int GetLevenshteinDistance(string source, string target)
    {
        if (string.IsNullOrEmpty(source)) return string.IsNullOrEmpty(target) ? 0 : target.Length;
        if (string.IsNullOrEmpty(target)) return source.Length;

        int[,] distance = new int[source.Length + 1, target.Length + 1];

        for (int i = 0; i <= source.Length; distance[i, 0] = i++) ;
        for (int j = 0; j <= target.Length; distance[0, j] = j++) ;

        for (int i = 1; i <= source.Length; i++)
        {
            for (int j = 1; j <= target.Length; j++)
            {
                int cost = (target[j - 1] == source[i - 1]) ? 0 : 1;
                distance[i, j] = Math.Min(
                    Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1),
                    distance[i - 1, j - 1] + cost);
            }
        }
        return distance[source.Length, target.Length];
    }
}
