using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SaverSearch.Application.Common.Interfaces;
using SaverSearch.Application.Common.Models.Pipeline;
using SaverSearch.Application.Common.Models.Pipeline.Planning;
using SaverSearch.Application.Common.Models.Pipeline.Ranking;
using SaverSearch.Application.Common.Models.Pipeline.Recommendations;
using SaverSearch.Application.Dtos.Discovery;

namespace SaverSearch.Application.Services;

public interface IDiscoveryService
{
    Task<DiscoveryResponse> DiscoverAsync(DiscoveryRequest request, CancellationToken cancellationToken = default);
}

public class DiscoveryService(
    IOfferDiscoveryPipeline pipeline,
    IRecommendationEngine recommendationEngine,
    IPurchasePlanningEngine planningEngine,
    IRankingEngine rankingEngine,
    IOfferNormalisationEngine normalisationEngine,
    ILogger<DiscoveryService> logger) : IDiscoveryService
{
    public async Task<DiscoveryResponse> DiscoverAsync(DiscoveryRequest request, CancellationToken cancellationToken = default)
    {
        var correlationId = request.CorrelationId ?? Guid.NewGuid().ToString("N")[..8];
        var sw = Stopwatch.StartNew();

        logger.LogInformation(
            "[{CorrelationId}] Discovery request started. Query={Query}, Goal={Goal}, Spend={Spend}",
            correlationId, request.Query, request.UserGoal, request.SpendAmount);

        try
        {
            // 1. Build DiscoveryContext from request
            var preferences = BuildPreferences(request);
            var context = new DiscoveryContext(
                UserId: null,
                RawQuery: request.Query,
                RetailerSlug: request.RetailerSlug,
                TargetSpend: request.SpendAmount ?? 100.0m,
                PaymentMethod: request.PaymentMethod,
                UserCardTier: null,
                UserRegion: request.UserRegion,
                Preferences: preferences
            );

            // 2. Execute the Offer Discovery Pipeline
            logger.LogInformation("[{CorrelationId}] Pipeline execution starting.", correlationId);
            var pipelineState = await pipeline.ExecuteAsync(context, cancellationToken);
            logger.LogInformation("[{CorrelationId}] Pipeline execution completed. Success={Success}",
                correlationId, pipelineState.Diagnostics.IsSuccess);

            if (!pipelineState.Diagnostics.IsSuccess)
            {
                sw.Stop();
                return BuildErrorResponse(
                    pipelineState.Diagnostics.ErrorMessage ?? "Pipeline execution failed.",
                    pipelineState.Diagnostics,
                    sw.ElapsedMilliseconds,
                    correlationId);
            }

            // 3. Normalise, rank, plan and recommend using engine outputs
            // If the pipeline has CalculatedOffers (from the Offer Resolver + Calculator stages), run
            // Normalisation → Ranking → Planning → Recommendation. Otherwise return an empty result.
            if (pipelineState.CalculatedOffers == null || !pipelineState.CalculatedOffers.Any())
            {
                sw.Stop();
                return BuildEmptyResponse(pipelineState.Diagnostics, sw.ElapsedMilliseconds, correlationId);
            }

            var normalisedOffers = await normalisationEngine.NormaliseOffersAsync(
                pipelineState.CalculatedOffers, context, cancellationToken);

            var rankedOffers = await rankingEngine.RankOffersAsync(normalisedOffers, context, cancellationToken);

            var purchasePlans = (await planningEngine.PlanPurchasesAsync(rankedOffers, context, cancellationToken)).ToList();

            if (purchasePlans.Count == 0)
            {
                sw.Stop();
                return BuildEmptyResponse(pipelineState.Diagnostics, sw.ElapsedMilliseconds, correlationId);
            }

            var decisionPackage = await recommendationEngine.RecommendBestPlanAsync(purchasePlans, context, cancellationToken);

            sw.Stop();
            logger.LogInformation("[{CorrelationId}] Discovery completed in {ElapsedMs}ms.", correlationId, sw.ElapsedMilliseconds);

            return BuildSuccessResponse(decisionPackage, purchasePlans, pipelineState.Diagnostics, sw.ElapsedMilliseconds, correlationId);
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            logger.LogWarning("[{CorrelationId}] Discovery request was cancelled after {ElapsedMs}ms.", correlationId, sw.ElapsedMilliseconds);
            return new DiscoveryResponse
            {
                Success = false,
                Errors = ["The request was cancelled."],
                ExecutionTimeMs = sw.ElapsedMilliseconds,
                CorrelationId = correlationId
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex, "[{CorrelationId}] Discovery request failed unexpectedly after {ElapsedMs}ms.", correlationId, sw.ElapsedMilliseconds);
            return new DiscoveryResponse
            {
                Success = false,
                Errors = ["An unexpected error occurred. Please try again."],
                ExecutionTimeMs = sw.ElapsedMilliseconds,
                CorrelationId = correlationId
            };
        }
    }

    // ──────────────────────────────────────────────
    // Private helpers
    // ──────────────────────────────────────────────

    private static IDictionary<string, string> BuildPreferences(DiscoveryRequest request)
    {
        var prefs = new Dictionary<string, string>();

        // Map UserGoal → UserIntent for strategy selection
        prefs["UserIntent"] = request.UserGoal switch
        {
            UserGoal.MaximumSavings => "MaximumSavings",
            UserGoal.LowestRisk => "LowestRisk",
            UserGoal.LowestComplexity => "FastestCheckout",
            UserGoal.HighestConfidence => "HighestConfidence",
            _ => "Balanced"
        };

        return prefs;
    }

    private static DiscoveryResponse BuildSuccessResponse(
        DecisionPackage package,
        List<PurchasePlan> allPlans,
        PipelineDiagnostics pipelineDiagnostics,
        long elapsedMs,
        string correlationId)
    {
        var primaryPlan = MapToPrimaryPlanDto(package);

        var alternatives = package.Alternatives
            .Select(a => new AlternativePlanDto(
                a.PurchasePlan.TotalExpectedSaving,
                a.PurchasePlan.OverallConfidence,
                a.RejectionReason))
            .ToList();

        var stageTimings = pipelineDiagnostics.Timings
            .Select(t => new StageTimingDto(t.StageName, t.ElapsedMilliseconds))
            .ToList();

        var diagnostics = new SearchDiagnosticsDto(
            elapsedMs,
            package.Diagnostics.StrategySelected,
            package.RecommendationType.ToString(),
            stageTimings,
            package.Warnings
        );

        return new DiscoveryResponse
        {
            Success = true,
            RecommendationTitle = package.Title,
            RecommendedPlan = primaryPlan,
            AlternativePlans = alternatives,
            Diagnostics = diagnostics,
            ExecutionTimeMs = elapsedMs,
            Warnings = package.Warnings,
            CorrelationId = correlationId
        };
    }

    private static PrimaryPlanDto MapToPrimaryPlanDto(DecisionPackage package)
    {
        var plan = package.PurchasePlan;

        var includedOffers = plan.IncludedOffers
            .Select(ro => new IncludedOfferSummaryDto(
                ro.NormalisedOffer.CalculatedOffer.Offer.Id,
                ro.NormalisedOffer.CalculatedOffer.Offer.Title,
                ro.NormalisedOffer.CalculatedOffer.Provider.Name,
                ro.NormalisedOffer.CalculatedOffer.Retailer.Name,
                ro.NormalisedOffer.ExpectedMonetaryValue))
            .ToList();

        var pathSteps = plan.PurchasePath
            .Select(s => new PurchasePathStepDto(s.StepNumber, s.ActionDescription, s.Explanation))
            .ToList();

        return new PrimaryPlanDto
        {
            EstimatedSaving = package.EstimatedSaving,
            GuaranteedSaving = package.GuaranteedSaving,
            MaximumSaving = package.MaximumSaving,
            Confidence = package.Confidence,
            RiskLevel = package.RiskLevel.ToString(),
            UserEffort = package.UserEffort,
            SelectionJustification = package.Reasoning.SelectionJustification,
            KeyStrengths = package.Reasoning.KeyStrengths,
            PotentialRisks = package.Reasoning.PotentialRisks,
            PurchasePath = pathSteps,
            IncludedOffers = includedOffers,
            RequiredUserActions = plan.RequiredUserActions,
            RequiredAccounts = plan.RequiredAccounts
        };
    }

    private static DiscoveryResponse BuildEmptyResponse(
        PipelineDiagnostics pipelineDiagnostics,
        long elapsedMs,
        string correlationId)
    {
        return new DiscoveryResponse
        {
            Success = true,
            RecommendationTitle = "No offers found",
            RecommendedPlan = null,
            AlternativePlans = [],
            Diagnostics = new SearchDiagnosticsDto(elapsedMs, "None", "None",
                pipelineDiagnostics.Timings.Select(t => new StageTimingDto(t.StageName, t.ElapsedMilliseconds)).ToList(),
                []),
            ExecutionTimeMs = elapsedMs,
            Warnings = ["No offers were found matching your query."],
            CorrelationId = correlationId
        };
    }

    private static DiscoveryResponse BuildErrorResponse(
        string errorMessage,
        PipelineDiagnostics pipelineDiagnostics,
        long elapsedMs,
        string correlationId)
    {
        return new DiscoveryResponse
        {
            Success = false,
            Errors = [errorMessage],
            Diagnostics = new SearchDiagnosticsDto(elapsedMs, "None", "None",
                pipelineDiagnostics.Timings.Select(t => new StageTimingDto(t.StageName, t.ElapsedMilliseconds)).ToList(),
                []),
            ExecutionTimeMs = elapsedMs,
            CorrelationId = correlationId
        };
    }
}
