using Microsoft.AspNetCore.Mvc;
using SaverSearch.Application.Dtos;

namespace SaverSearch.Api.Controllers;

public class OffersController : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<OfferDto>>> Search([FromQuery] string query, CancellationToken cancellationToken)
    {
        // Future-proof: Calls application query handler and returns REST DTOs
        await Task.CompletedTask;
        return Ok(Enumerable.Empty<OfferDto>());
    }
}
