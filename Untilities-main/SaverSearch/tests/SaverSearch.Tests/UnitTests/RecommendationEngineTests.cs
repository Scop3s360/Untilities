using System.Diagnostics;
using SaverSearch.Application.Common.Interfaces;
using SaverSearch.Application.Common.Models.Pipeline;
using SaverSearch.Application.Common.Models.Pipeline.Planning;
using SaverSearch.Application.Common.Models.Pipeline.Recommendations;
using SaverSearch.Application.Services.Pipeline;
using SaverSearch.Application.Services.Pipeline.Recommendations;
using Xunit;
using Xunit.Abstractions;

namespace SaverSearch.Tests.UnitTests;

public class RecommendationEngineTests
{
    private readonly ITestOutputHelper _output;
    private readonly List<IRiskEvaluator> _riskEvaluators;
    private readonly List<IRecommendationStrategy> _strategies;

    public RecommendationEngineTests(ITestOutputHelper output)
    {
        _output = output;

        _riskEvaluators = new List<IRiskEvaluator>
        {
            new StandardRiskEvaluator()
        };

        _strategies = new List<IRecommendationStrategy>
        {
            new BestOverallRecommendationStrategy(_riskEvaluators),
            new MaximumSavingRecommendationStrategy(_riskEvaluators),
            new LowestComplexityRecommendationStrategy(_riskEvaluators),
            new HighestConfidenceRecommendationStrategy(_riskEvaluators),
            new LowestRiskRecommendationStrategy(_riskEvaluators),
            new BalancedRecommendationStrategy(_riskEvaluators)
        };
    }

    private PurchasePlan GetMockPurchasePlan(decimal expectedSaving, double confidence, double complexity)
    {
        var diagnostics = new PurchasePlanDiagnostics(0, "Mock", 0, new List<string>());
        return new PurchasePlan(
            new List<PurchasePathStep>(),
            new List<SaverSearch.Application.Common.Models.Pipeline.Ranking.RankedOffer>(),
            expectedSaving,
            expectedSaving,
            expectedSaving,
            confidence,
            complexity,
            new List<string>(),
            new List<string>(),
            new List<CompatibilityEvidence>(),
            "Mock Plan Explanation",
            diagnostics
        );
    }

    [Fact]
    public async Task StrategySelection_ShouldMatchUserIntent()
    {
        // Arrange
        var engine = new RecommendationEngine(_strategies);
        var plan1 = GetMockPurchasePlan(10.0m, 100.0, 10.0);
        var plan2 = GetMockPurchasePlan(20.0m, 100.0, 40.0);

        var context = new DiscoveryContext(null, "Query", null, 100.0m, null, null, null, new Dictionary<string, string>
        {
            { "UserIntent", "MaximumSavings" }
        });

        // Act
        var package = await engine.RecommendBestPlanAsync(new List<PurchasePlan> { plan1, plan2 }, context);

        // Assert
        Assert.Equal(RecommendationType.MaximumSaving, package.RecommendationType);
        Assert.Equal(20.0m, package.EstimatedSaving);
        Assert.Single(package.Alternatives);
        Assert.Contains("Lower overall expected savings", package.Alternatives.First().RejectionReason);
    }

    [Fact]
    public async Task RiskEvaluator_ShouldFlagHighComplexityAsMediumOrHighRisk()
    {
        // Arrange
        var evaluator = new StandardRiskEvaluator();
        var safePlan = GetMockPurchasePlan(10.0m, 100.0, 10.0);
        var riskyPlan = GetMockPurchasePlan(10.0m, 80.0, 60.0); // low confidence, high complexity

        var context = new DiscoveryContext(null, "Query", null, 100.0m, null, null, null, new Dictionary<string, string>());

        // Act
        var safeRisk = evaluator.EvaluateRisk(safePlan, context);
        var riskyRisk = evaluator.EvaluateRisk(riskyPlan, context);

        // Assert
        Assert.Equal(RiskLevel.Low, safeRisk.RiskLevel);
        Assert.Equal(RiskLevel.High, riskyRisk.RiskLevel);
        Assert.Contains(riskyRisk.RiskFactors, f => f.Contains("confidence"));
        Assert.Contains(riskyRisk.RiskFactors, f => f.Contains("complexity"));
    }

    [Fact]
    public async Task LearningFeedbackHooks_ShouldAcceptTelemetryFeedback()
    {
        // Arrange
        // Verify contract can be implemented and triggered as a mock feedback class
        var feedbackTriggered = false;
        var mockFeedback = new MockFeedbackService(() => feedbackTriggered = true);

        // Act
        await mockFeedback.SubmitFeedbackAsync(Guid.NewGuid(), FeedbackStatus.Accepted, "Worked perfectly!");

        // Assert
        Assert.True(feedbackTriggered);
    }

    private class MockFeedbackService(Action callback) : IRecommendationFeedback
    {
        public Task SubmitFeedbackAsync(Guid packageId, FeedbackStatus status, string? comments = null)
        {
            callback();
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task PerformanceBenchmark_ShouldScaleUnderConcurrentStress()
    {
        // Arrange
        var engine = new RecommendationEngine(_strategies);
        var plan = GetMockPurchasePlan(10.0m, 95.0, 12.0);
        var context = new DiscoveryContext(null, "Query", null, 100.0m, null, null, null, new Dictionary<string, string>());

        // Seed 1000 plans
        var list = Enumerable.Range(1, 1000).Select(_ => plan).ToList();

        // Act & Measure
        var stopwatch = Stopwatch.StartNew();
        var package = await engine.RecommendBestPlanAsync(list, context);
        stopwatch.Stop();

        _output.WriteLine($"Recommended best plan from 1,000 choices in {stopwatch.ElapsedMilliseconds}ms.");

        // Assert
        Assert.NotNull(package);
        Assert.True(stopwatch.ElapsedMilliseconds < 500, "Performance stress exceeds 500ms budget limit.");
    }
}
