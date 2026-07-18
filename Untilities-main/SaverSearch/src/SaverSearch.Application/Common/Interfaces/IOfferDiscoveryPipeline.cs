using SaverSearch.Application.Common.Models.Pipeline;

namespace SaverSearch.Application.Common.Interfaces;

public interface IOfferDiscoveryPipeline
{
    Task<PipelineState> ExecuteAsync(DiscoveryContext context, CancellationToken cancellationToken = default);
}
