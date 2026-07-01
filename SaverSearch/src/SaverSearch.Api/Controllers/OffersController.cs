using Microsoft.AspNetCore.Mvc;
using SaverSearch.Application.Common.Interfaces;
using SaverSearch.Application.Common.Models;
using SaverSearch.Application.Dtos;

namespace SaverSearch.Api.Controllers;

[Produces("application/json")]
public class OffersController(IOfferService offerService) : BaseApiController
{
    /// <summary>
    /// Retrieves a paginated list of offers based on filters.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<PaginatedList<OfferDto>>))]
    public async Task<ActionResult<ApiResponse<PaginatedList<OfferDto>>>> GetPaged(
        [FromQuery] OfferQueryParameters query, 
        CancellationToken cancellationToken)
    {
        var result = await offerService.GetPagedAsync(query, cancellationToken);
        return Ok(ApiResponse<PaginatedList<OfferDto>>.SuccessResponse(result));
    }

    /// <summary>
    /// Retrieves a specific offer by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<OfferDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ApiResponse<object>))]
    public async Task<ActionResult<ApiResponse<OfferDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var offer = await offerService.GetByIdAsync(id, cancellationToken);
        if (offer == null) 
            return NotFound(ApiResponse<object>.ErrorResponse("Offer not found."));
            
        return Ok(ApiResponse<OfferDto>.SuccessResponse(offer));
    }

    /// <summary>
    /// Creates a new offer.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(ApiResponse<OfferDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ApiResponse<object>))]
    public async Task<ActionResult<ApiResponse<OfferDto>>> Create(CreateOfferDto dto, CancellationToken cancellationToken)
    {
        var created = await offerService.CreateAsync(dto, cancellationToken);
        var response = ApiResponse<OfferDto>.SuccessResponse(created, "Offer created successfully.");
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, response);
    }

    /// <summary>
    /// Updates an existing offer by ID.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ApiResponse<object>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ApiResponse<object>))]
    public async Task<IActionResult> Update(Guid id, UpdateOfferDto dto, CancellationToken cancellationToken)
    {
        var succeeded = await offerService.UpdateAsync(id, dto, cancellationToken);
        if (!succeeded) 
            return NotFound(ApiResponse<object>.ErrorResponse("Offer not found."));
            
        return NoContent();
    }

    /// <summary>
    /// Deletes an offer by ID.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ApiResponse<object>))]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var succeeded = await offerService.DeleteAsync(id, cancellationToken);
        if (!succeeded) 
            return NotFound(ApiResponse<object>.ErrorResponse("Offer not found."));
            
        return NoContent();
    }
}
