using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SaverSearch.Application.Common.Interfaces;
using SaverSearch.Application.Common.Models.Pipeline;
using SaverSearch.Application.Services.Pipeline;
using Xunit;

namespace SaverSearch.Tests.UnitTests;

public class PipelineTests
{
    private readonly NullLogger<OfferDiscoveryPipeline> _nullLogger = NullLogger<OfferDiscoveryPipeline>.Instance;

    [Fact]
    public async Task Pipeline_ShouldExecuteStages_InSequenceOrder()
    {
        // Arrange
        var stage1 = new MockStage("First", 1);
        var stage2 = new MockStage("Second", 2);
        var stage3 = new MockStage("Third", 3);

        // Put them in unordered list to test sorting
        var pipeline = new OfferDiscoveryPipeline(new IPipelineStage[] { stage3, stage1, stage2 }, _nullLogger);
        var context = new DiscoveryContext(Guid.NewGuid(), "Amazon", null, 50.0m, null, null, null, new Dictionary<string, string>());

        // Act
        var result = await pipeline.ExecuteAsync(context);

        // Assert
        Assert.True(result.Diagnostics.IsSuccess);
        Assert.Equal(3, result.Diagnostics.Timings.Count);
        Assert.Equal("First", result.Diagnostics.Timings[0].StageName);
        Assert.Equal("Second", result.Diagnostics.Timings[1].StageName);
        Assert.Equal("Third", result.Diagnostics.Timings[2].StageName);
    }

    [Fact]
    public async Task Pipeline_ShouldStopExecution_WhenStageThrowsException()
    {
        // Arrange
        var stage1 = new MockStage("First", 1);
        var failingStage = new FailingMockStage("Failing", 2);
        var stage3 = new MockStage("Third", 3);

        var pipeline = new OfferDiscoveryPipeline(new IPipelineStage[] { stage1, failingStage, stage3 }, _nullLogger);
        var context = new DiscoveryContext(Guid.NewGuid(), "Tesco", null, 10.0m, null, null, null, new Dictionary<string, string>());

        // Act
        var result = await pipeline.ExecuteAsync(context);

        // Assert
        Assert.False(result.Diagnostics.IsSuccess);
        Assert.Equal("Failing", result.Diagnostics.FailedStage);
        Assert.Equal("Stage failed intentionally.", result.Diagnostics.ErrorMessage);
        // Assert stage 3 was NOT run
        Assert.Single(result.Diagnostics.Timings);
        Assert.Equal("First", result.Diagnostics.Timings[0].StageName);
    }

    [Fact]
    public async Task Pipeline_ShouldStopExecution_WhenCancelled()
    {
        // Arrange
        var stage1 = new MockStage("First", 1);
        var stage2 = new MockStage("Second", 2);
        
        var pipeline = new OfferDiscoveryPipeline(new IPipelineStage[] { stage1, stage2 }, _nullLogger);
        var context = new DiscoveryContext(Guid.NewGuid(), "Currys", null, 100.0m, null, null, null, new Dictionary<string, string>());

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act
        var result = await pipeline.ExecuteAsync(context, cts.Token);

        // Assert
        Assert.False(result.Diagnostics.IsSuccess);
        Assert.Equal("First", result.Diagnostics.FailedStage);
        Assert.Equal("Operation was cancelled.", result.Diagnostics.ErrorMessage);
        Assert.Empty(result.Diagnostics.Timings);
    }

    private class MockStage(string name, int sequence) : IPipelineStage
    {
        public string StageName => name;
        public int Sequence => sequence;

        public Task<PipelineState> ExecuteAsync(PipelineState state, CancellationToken cancellationToken)
        {
            return Task.FromResult(state);
        }
    }

    private class FailingMockStage(string name, int sequence) : IPipelineStage
    {
        public string StageName => name;
        public int Sequence => sequence;

        public Task<PipelineState> ExecuteAsync(PipelineState state, CancellationToken cancellationToken)
        {
            throw new Exception("Stage failed intentionally.");
        }
    }
}
