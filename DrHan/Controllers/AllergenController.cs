using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using DrHan.Application.Services.AllergenServices.Commands.CreateAllergen;
using DrHan.Application.Services.AllergenServices.Commands.UpdateAllergen;
using DrHan.Application.Services.AllergenServices.Commands.DeleteAllergen;
using DrHan.Application.Services.AllergenServices.Queries.GetAllAllergens;
using DrHan.Application.Services.AllergenServices.Queries.GetAllergenById;
using DrHan.Application.Services.AllergenServices.Queries.SearchAllergens;
using DrHan.Application.Services.AllergenServices.Queries.GetAllergenCategories;
using DrHan.Application.Services.AllergenServices.Queries.GetAllergensByCategory;

namespace DrHan.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AllergenController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AllergenController> _logger;

    public AllergenController(IMediator mediator, ILogger<AllergenController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get all allergens
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllAllergens()
    {
        try
        {
            var query = new GetAllAllergensQuery();
            var response = await _mediator.Send(query);
            
            if (!response.IsSucceeded)
                return BadRequest(response);
                
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all allergens");
            return StatusCode(500, "An error occurred while retrieving allergens");
        }
    }

    /// <summary>
    /// Get allergen by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetAllergenById(int id)
    {
        try
        {
            var query = new GetAllergenByIdQuery { Id = id };
            var response = await _mediator.Send(query);
            
            if (!response.IsSucceeded)
                return BadRequest(response);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving allergen with ID {Id}", id);
            return StatusCode(500, "An error occurred while retrieving the allergen");
        }
    }

    /// <summary>
    /// Create a new allergen
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateAllergen([FromBody] CreateAllergenCommand command)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var response = await _mediator.Send(command);
            
            if (!response.IsSucceeded)
                return BadRequest(response);
                
            return CreatedAtAction(nameof(GetAllergenById), new { id = response.Data!.Id }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating allergen");
            return StatusCode(500, "An error occurred while creating the allergen");
        }
    }

    /// <summary>
    /// Update an existing allergen
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateAllergen(int id, [FromBody] UpdateAllergenCommand command)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            command.Id = id;
            var response = await _mediator.Send(command);
            
            if (!response.IsSucceeded)
                return BadRequest(response);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating allergen with ID {Id}", id);
            return StatusCode(500, "An error occurred while updating the allergen");
        }
    }

    /// <summary>
    /// Delete an allergen
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteAllergen(int id)
    {
        try
        {
            var command = new DeleteAllergenCommand { Id = id };
            var response = await _mediator.Send(command);
            
            if (!response.IsSucceeded)
                return BadRequest(response);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting allergen with ID {Id}", id);
            return StatusCode(500, "An error occurred while deleting the allergen");
        }
    }

    /// <summary>
    /// Search allergens by name, scientific name, or description
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> SearchAllergens([FromQuery] string searchTerm)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return BadRequest("Search term cannot be empty");

            var query = new SearchAllergensQuery { SearchTerm = searchTerm };
            var response = await _mediator.Send(query);
            
            if (!response.IsSucceeded)
                return BadRequest(response);
                
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching allergens with term {SearchTerm}", searchTerm);
            return StatusCode(500, "An error occurred while searching allergens");
        }
    }

    /// <summary>
    /// Get all allergen categories
    /// </summary>
    [HttpGet("categories")]
    public async Task<IActionResult> GetAllergenCategories()
    {
        try
        {
            var query = new GetAllergenCategoriesQuery();
            var response = await _mediator.Send(query);
            
            if (!response.IsSucceeded)
                return BadRequest(response);
                
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving allergen categories");
            return StatusCode(500, "An error occurred while retrieving allergen categories");
        }
    }

    /// <summary>
    /// Get allergens by category
    /// </summary>
    [HttpGet("category/{category}")]
    public async Task<IActionResult> GetAllergensByCategory(string category)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(category))
                return BadRequest("Category parameter cannot be empty");

            var query = new GetAllergensByCategoryQuery { Category = category };
            var response = await _mediator.Send(query);
            
            if (!response.IsSucceeded)
                return BadRequest(response);
                
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving allergens for category {Category}", category);
            return StatusCode(500, "An error occurred while retrieving allergens by category");
        }
    }
} 