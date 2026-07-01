namespace SaverSearch.Application.Common.Models.Pipeline;

public interface IPipelineStage
{
    string StageName { get; }
    int Sequence { get; }
    Task<PipelineState> ExecuteAsync(PipelineState state, CancellationToken cancellationToken);
}
