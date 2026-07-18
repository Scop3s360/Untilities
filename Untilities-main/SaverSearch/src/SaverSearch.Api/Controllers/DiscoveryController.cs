using Microsoft.AspNetCore.Mvc;
using SaverSearch.Application.Common.Models;
using SaverSearch.Application.Dtos.Discovery;
using SaverSearch.Application.Services;

namespace SaverSearch.Api.Controllers;

/// <summary>
/// Offer Discovery endpoint.
/// Accepts a search query and returns ranked, stacked and recommended purchase plans.
/// </summary>
[Produces("application/json")]
public class DiscoveryController(
    IDiscoveryService discoveryService,
    ILogger<DiscoveryController> logger) : BaseApiController
{
    /// <summary>
    /// Discover the best offers for a given query.
    /// </summary>
    /// <remarks>
    /// Executes the full Offer Discovery Pipeline:
    /// Retailer Resolution → Offer Resolution → Rules Evaluation → Savings Calculation
    /// → Normalisation → Ranking → Purchase Planning → Recommendation.
    ///
    /// Returns the primary recommended purchase plan with diagnostics and alternatives.
    /// </remarks>
    /// <param name="request">The discovery query and user preferences.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A recommendation package with the best matched offers and purchase path.</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<DiscoveryResponse>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ApiResponse<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ApiResponse<object>))]
    public async Task<ActionResult<ApiResponse<DiscoveryResponse>>> Discover(
        [FromBody] DiscoveryRequest request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Discovery endpoint called. Query={Query}, Goal={Goal}",
            request.Query,
            request.UserGoal);

        var response = await discoveryService.DiscoverAsync(request, cancellationToken);

        if (!response.Success)
        {
            logger.LogWarning(
                "Discovery returned a failure result. CorrelationId={CorrelationId} Errors={Errors}",
                response.CorrelationId,
                string.Join(", ", response.Errors));

            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<DiscoveryResponse>.ErrorResponse(
                    "Discovery failed. Please try again.",
                    response.Errors));
        }

        return Ok(ApiResponse<DiscoveryResponse>.SuccessResponse(response));
    }
}
