using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SaverSearch.Application.Common.Interfaces;
using SaverSearch.Application.Common.Models.Pipeline;

namespace SaverSearch.Application.Services.Pipeline;

public class OfferDiscoveryPipeline(
    IEnumerable<IPipelineStage> stages,
    ILogger<OfferDiscoveryPipeline> logger) : IOfferDiscoveryPipeline
{
    private readonly List<IPipelineStage> _stages = stages.OrderBy(s => s.Sequence).ToList();

    public async Task<PipelineState> ExecuteAsync(DiscoveryContext context, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Offer Discovery Pipeline: Started processing request for user {UserId}", context.UserId);

        var state = new PipelineState(context);
        var timings = new List<StageTiming>();

        foreach (var stage in _stages)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning("Offer Discovery Pipeline: Cancelled before executing stage {StageName}", stage.StageName);
                
                var cancelDiagnostics = new PipelineDiagnostics
                {
                    Timings = timings,
                    IsSuccess = false,
                    FailedStage = stage.StageName,
                    ErrorMessage = "Operation was cancelled."
                };
                return state with { Diagnostics = cancelDiagnostics };
            }

            logger.LogInformation("Offer Discovery Pipeline: Starting stage {StageName} (Sequence {Sequence})", stage.StageName, stage.Sequence);

            var stopwatch = Stopwatch.StartNew();

            try
            {
                state = await stage.ExecuteAsync(state, cancellationToken);
                stopwatch.Stop();

                logger.LogInformation(
                    "Offer Discovery Pipeline: Completed stage {StageName} in {ElapsedMs}ms",
                    stage.StageName,
                    stopwatch.ElapsedMilliseconds
                );

                timings.Add(new StageTiming(stage.StageName, stopwatch.ElapsedMilliseconds));
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                
                logger.LogError(
                    ex,
                    "Offer Discovery Pipeline: Failed at stage {StageName} after {ElapsedMs}ms",
                    stage.StageName,
                    stopwatch.ElapsedMilliseconds
                );

                var errorDiagnostics = new PipelineDiagnostics
                {
                    Timings = timings,
                    IsSuccess = false,
                    FailedStage = stage.StageName,
                    ErrorMessage = ex.Message
                };

                return state with { Diagnostics = errorDiagnostics };
            }
        }

        var successDiagnostics = new PipelineDiagnostics
        {
            Timings = timings,
            IsSuccess = true
        };

        logger.LogInformation(
            "Offer Discovery Pipeline: Completed successfully in {TotalElapsedMs}ms",
            timings.Sum(t => t.ElapsedMilliseconds)
        );

        return state with { Diagnostics = successDiagnostics };
    }
}
