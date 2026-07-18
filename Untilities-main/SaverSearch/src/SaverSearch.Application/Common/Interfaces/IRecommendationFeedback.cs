using SaverSearch.Application.Common.Models.Pipeline.Recommendations;

namespace SaverSearch.Application.Common.Interfaces;

public interface IRecommendationFeedback
{
    Task SubmitFeedbackAsync(Guid packageId, FeedbackStatus status, string? comments = null);
}
// This provides an extensibility point for telemetry and learning loop algorithms.
