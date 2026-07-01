using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using SaverSearch.Application.Common.Interfaces;
using SaverSearch.Application.Common.Models.Pipeline;
using SaverSearch.Application.Common.Models.Resolver;
using SaverSearch.Domain.Entities;

namespace SaverSearch.Application.Services.Resolver;

public class RetailerResolver(
    IUnitOfWork unitOfWork,
    IEnumerable<IRetailerResolverStrategy> strategies) : IRetailerResolver
{
    private readonly List<IRetailerResolverStrategy> _strategies = strategies.ToList();

    public async Task<RetailerResolverResponse> ResolveAsync(DiscoveryContext context, CancellationToken cancellationToken = default)
    {
        // 1. Fetch active retailers including their aliases
        var query = unitOfWork.Retailers.GetQueryable(asNoTracking: true)
            .Include(r => r.Aliases)
            .Where(r => r.IsActive);

        List<Retailer> retailers;
        if (query is IAsyncEnumerable<Retailer>)
        {
            retailers = await query.ToListAsync(cancellationToken);
        }
        else
        {
            retailers = query.ToList();
        }

        var executedStrategies = new List<StrategyTiming>();
        var allMatches = new List<ResolverResult>();

        // 2. Execute each strategy sequentially measuring time
        foreach (var strategy in _strategies)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var matches = await strategy.MatchAsync(context, retailers, cancellationToken);
                stopwatch.Stop();

                executedStrategies.Add(new StrategyTiming(strategy.StrategyName, stopwatch.ElapsedMilliseconds));
                allMatches.AddRange(matches);
            }
            catch
            {
                stopwatch.Stop();
                executedStrategies.Add(new StrategyTiming(strategy.StrategyName, stopwatch.ElapsedMilliseconds));
                // Suppress strategy failure to keep pipeline alive
            }
        }

        // 3. Score and group results (deduplicate by Retailer ID, taking the highest score)
        var groupedMatches = allMatches
            .GroupBy(m => m.Retailer.Id)
            .Select(g => g.OrderByDescending(m => m.ConfidenceScore).First())
            .OrderByDescending(m => m.ConfidenceScore)
            .ToList();

        if (groupedMatches.Count == 0)
        {
            var failedDiagnostics = new ResolverDiagnostics(
                executedStrategies,
                "None",
                "No strategy resolved any matching retailer."
            );
            return new RetailerResolverResponse(null, 0.0, "None", Enumerable.Empty<ResolverResult>(), failedDiagnostics);
        }

        // 4. Extract winner and alternatives
        var winner = groupedMatches[0];
        var alternatives = groupedMatches.Skip(1);

        var successDiagnostics = new ResolverDiagnostics(
            executedStrategies,
            winner.MatchType,
            winner.Reason
        );

        return new RetailerResolverResponse(
            winner.Retailer,
            winner.ConfidenceScore,
            winner.MatchType,
            alternatives,
            successDiagnostics
        );
    }
}
