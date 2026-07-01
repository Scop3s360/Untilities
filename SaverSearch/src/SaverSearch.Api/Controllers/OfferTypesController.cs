using Microsoft.AspNetCore.Mvc;
using SaverSearch.Application.Common.Interfaces;
using SaverSearch.Application.Common.Models;
using SaverSearch.Application.Dtos;

namespace SaverSearch.Api.Controllers;

[Produces("application/json")]
public class OfferTypesController(IOfferTypeService offerTypeService) : BaseApiController
{
    /// <summary>
    /// Retrieves a paginated list of offer types based on filters.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<PaginatedList<OfferTypeDto>>))]
    public async Task<ActionResult<ApiResponse<PaginatedList<OfferTypeDto>>>> GetPaged(
        [FromQuery] OfferTypeQueryParameters query, 
        CancellationToken cancellationToken)
    {
        var result = await offerTypeService.GetPagedAsync(query, cancellationToken);
        return Ok(ApiResponse<PaginatedList<OfferTypeDto>>.SuccessResponse(result));
    }

    /// <summary>
    /// Retrieves a specific offer type by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<OfferTypeDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ApiResponse<object>))]
    public async Task<ActionResult<ApiResponse<OfferTypeDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var type = await offerTypeService.GetByIdAsync(id, cancellationToken);
        if (type == null) 
            return NotFound(ApiResponse<object>.ErrorResponse("OfferType not found."));
            
        return Ok(ApiResponse<OfferTypeDto>.SuccessResponse(type));
    }

    /// <summary>
    /// Creates a new offer type.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(ApiResponse<OfferTypeDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ApiResponse<object>))]
    public async Task<ActionResult<ApiResponse<OfferTypeDto>>> Create(CreateOfferTypeDto dto, CancellationToken cancellationToken)
    {
        var created = await offerTypeService.CreateAsync(dto, cancellationToken);
        var response = ApiResponse<OfferTypeDto>.SuccessResponse(created, "OfferType created successfully.");
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, response);
    }

    /// <summary>
    /// Updates an existing offer type by ID.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ApiResponse<object>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ApiResponse<object>))]
    public async Task<IActionResult> Update(Guid id, UpdateOfferTypeDto dto, CancellationToken cancellationToken)
    {
        var succeeded = await offerTypeService.UpdateAsync(id, dto, cancellationToken);
        if (!succeeded) 
            return NotFound(ApiResponse<object>.ErrorResponse("OfferType not found."));
            
        return NoContent();
    }

    /// <summary>
    /// Deletes an offer type by ID.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ApiResponse<object>))]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var succeeded = await offerTypeService.DeleteAsync(id, cancellationToken);
        if (!succeeded) 
            return NotFound(ApiResponse<object>.ErrorResponse("OfferType not found."));
            
        return NoContent();
    }
}
