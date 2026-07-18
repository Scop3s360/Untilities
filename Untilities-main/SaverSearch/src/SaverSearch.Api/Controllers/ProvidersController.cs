using Microsoft.AspNetCore.Mvc;
using SaverSearch.Application.Common.Interfaces;
using SaverSearch.Application.Common.Models;
using SaverSearch.Application.Dtos;

namespace SaverSearch.Api.Controllers;

[Produces("application/json")]
public class ProvidersController(IProviderService providerService) : BaseApiController
{
    /// <summary>
    /// Retrieves a paginated list of providers based on filters.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<PaginatedList<ProviderDto>>))]
    public async Task<ActionResult<ApiResponse<PaginatedList<ProviderDto>>>> GetPaged(
        [FromQuery] ProviderQueryParameters query, 
        CancellationToken cancellationToken)
    {
        var result = await providerService.GetPagedAsync(query, cancellationToken);
        return Ok(ApiResponse<PaginatedList<ProviderDto>>.SuccessResponse(result));
    }

    /// <summary>
    /// Retrieves a specific provider by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<ProviderDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ApiResponse<object>))]
    public async Task<ActionResult<ApiResponse<ProviderDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var provider = await providerService.GetByIdAsync(id, cancellationToken);
        if (provider == null) 
            return NotFound(ApiResponse<object>.ErrorResponse("Provider not found."));
            
        return Ok(ApiResponse<ProviderDto>.SuccessResponse(provider));
    }

    /// <summary>
    /// Creates a new provider.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(ApiResponse<ProviderDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ApiResponse<object>))]
    public async Task<ActionResult<ApiResponse<ProviderDto>>> Create(CreateProviderDto dto, CancellationToken cancellationToken)
    {
        var created = await providerService.CreateAsync(dto, cancellationToken);
        var response = ApiResponse<ProviderDto>.SuccessResponse(created, "Provider created successfully.");
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, response);
    }

    /// <summary>
    /// Updates an existing provider by ID.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ApiResponse<object>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ApiResponse<object>))]
    public async Task<IActionResult> Update(Guid id, UpdateProviderDto dto, CancellationToken cancellationToken)
    {
        var succeeded = await providerService.UpdateAsync(id, dto, cancellationToken);
        if (!succeeded) 
            return NotFound(ApiResponse<object>.ErrorResponse("Provider not found."));
            
        return NoContent();
    }

    /// <summary>
    /// Deletes a provider by ID.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ApiResponse<object>))]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var succeeded = await providerService.DeleteAsync(id, cancellationToken);
        if (!succeeded) 
            return NotFound(ApiResponse<object>.ErrorResponse("Provider not found."));
            
        return NoContent();
    }
}
