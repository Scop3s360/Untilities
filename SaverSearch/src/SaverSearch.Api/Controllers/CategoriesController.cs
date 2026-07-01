using Microsoft.AspNetCore.Mvc;
using SaverSearch.Application.Common.Interfaces;
using SaverSearch.Application.Common.Models;
using SaverSearch.Application.Dtos;

namespace SaverSearch.Api.Controllers;

[Produces("application/json")]
public class CategoriesController(ICategoryService categoryService) : BaseApiController
{
    /// <summary>
    /// Retrieves a paginated list of categories based on filters.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<PaginatedList<CategoryDto>>))]
    public async Task<ActionResult<ApiResponse<PaginatedList<CategoryDto>>>> GetPaged(
        [FromQuery] CategoryQueryParameters query, 
        CancellationToken cancellationToken)
    {
        var result = await categoryService.GetPagedAsync(query, cancellationToken);
        return Ok(ApiResponse<PaginatedList<CategoryDto>>.SuccessResponse(result));
    }

    /// <summary>
    /// Retrieves a specific category by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<CategoryDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ApiResponse<object>))]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var category = await categoryService.GetByIdAsync(id, cancellationToken);
        if (category == null) 
            return NotFound(ApiResponse<object>.ErrorResponse("Category not found."));
            
        return Ok(ApiResponse<CategoryDto>.SuccessResponse(category));
    }

    /// <summary>
    /// Creates a new category.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(ApiResponse<CategoryDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ApiResponse<object>))]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> Create(CreateCategoryDto dto, CancellationToken cancellationToken)
    {
        var created = await categoryService.CreateAsync(dto, cancellationToken);
        var response = ApiResponse<CategoryDto>.SuccessResponse(created, "Category created successfully.");
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, response);
    }

    /// <summary>
    /// Updates an existing category by ID.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ApiResponse<object>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ApiResponse<object>))]
    public async Task<IActionResult> Update(Guid id, UpdateCategoryDto dto, CancellationToken cancellationToken)
    {
        var succeeded = await categoryService.UpdateAsync(id, dto, cancellationToken);
        if (!succeeded) 
            return NotFound(ApiResponse<object>.ErrorResponse("Category not found."));
            
        return NoContent();
    }

    /// <summary>
    /// Deletes a category by ID.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ApiResponse<object>))]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var succeeded = await categoryService.DeleteAsync(id, cancellationToken);
        if (!succeeded) 
            return NotFound(ApiResponse<object>.ErrorResponse("Category not found."));
            
        return NoContent();
    }
}
