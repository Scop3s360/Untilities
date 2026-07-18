using Microsoft.AspNetCore.Mvc;
using SaverSearch.Application.Common.Interfaces;
using SaverSearch.Application.Common.Models;
using SaverSearch.Application.Dtos;

namespace SaverSearch.Api.Controllers;

[Produces("application/json")]
public class RetailersController(IRetailerService retailerService) : BaseApiController
{
    /// <summary>
    /// Retrieves a paginated list of retailers based on filters.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<PaginatedList<RetailerDto>>))]
    public async Task<ActionResult<ApiResponse<PaginatedList<RetailerDto>>>> GetPaged(
        [FromQuery] RetailerQueryParameters query, 
        CancellationToken cancellationToken)
    {
        var result = await retailerService.GetPagedAsync(query, cancellationToken);
        return Ok(ApiResponse<PaginatedList<RetailerDto>>.SuccessResponse(result));
    }

    /// <summary>
    /// Retrieves a specific retailer by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<RetailerDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ApiResponse<object>))]
    public async Task<ActionResult<ApiResponse<RetailerDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var retailer = await retailerService.GetByIdAsync(id, cancellationToken);
        if (retailer == null) 
            return NotFound(ApiResponse<object>.ErrorResponse("Retailer not found."));
            
        return Ok(ApiResponse<RetailerDto>.SuccessResponse(retailer));
    }

    /// <summary>
    /// Creates a new retailer.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(ApiResponse<RetailerDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ApiResponse<object>))]
    public async Task<ActionResult<ApiResponse<RetailerDto>>> Create(CreateRetailerDto dto, CancellationToken cancellationToken)
    {
        var created = await retailerService.CreateAsync(dto, cancellationToken);
        var response = ApiResponse<RetailerDto>.SuccessResponse(created, "Retailer created successfully.");
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, response);
    }

    /// <summary>
    /// Updates an existing retailer by ID.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ApiResponse<object>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ApiResponse<object>))]
    public async Task<IActionResult> Update(Guid id, UpdateRetailerDto dto, CancellationToken cancellationToken)
    {
        var succeeded = await retailerService.UpdateAsync(id, dto, cancellationToken);
        if (!succeeded) 
            return NotFound(ApiResponse<object>.ErrorResponse("Retailer not found."));
            
        return NoContent();
    }

    /// <summary>
    /// Deletes a retailer by ID.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ApiResponse<object>))]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var succeeded = await retailerService.DeleteAsync(id, cancellationToken);
        if (!succeeded) 
            return NotFound(ApiResponse<object>.ErrorResponse("Retailer not found."));
            
        return NoContent();
    }
}
