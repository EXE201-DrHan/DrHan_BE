using DrHan.Application.Commons;
using DrHan.Application.DTOs.Recipes;
using DrHan.Application.Services.RecipeServices.Queries.GetRecipeById;
using DrHan.Application.Services.RecipeServices.Queries.SearchRecipes;
using DrHan.Application.Services.RecipeServices.Queries.GetRecipeFilterOptions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DrHan.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RecipesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<RecipesController> _logger;

    public RecipesController(IMediator mediator, ILogger<RecipesController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Search recipes with filtering, sorting, and pagination
    /// </summary>
    /// <param name="searchDto">Search parameters</param>
    /// <returns>Paginated list of recipes</returns>
    [HttpGet("search")]
    [ProducesResponseType(typeof(AppResponse<IPaginatedList<RecipeDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AppResponse<IPaginatedList<RecipeDto>>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(AppResponse<IPaginatedList<RecipeDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AppResponse<IPaginatedList<RecipeDto>>>> SearchRecipes([FromQuery] RecipeSearchDto searchDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                    );

                var response = new AppResponse<IPaginatedList<RecipeDto>>()
                    .SetErrorResponse(errors);

                return BadRequest(response);
            }

            var query = new SearchRecipesQuery(searchDto);
            var result = await _mediator.Send(query);

            if (result.IsSucceeded)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SearchRecipes endpoint");
            var errorResponse = new AppResponse<IPaginatedList<RecipeDto>>()
                .SetErrorResponse("Error", "An unexpected error occurred while searching recipes");
            return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
        }
    }

    /// <summary>
    /// Get a recipe by its ID with full details
    /// </summary>
    /// <param name="id">Recipe ID</param>
    /// <returns>Recipe details</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AppResponse<RecipeDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AppResponse<RecipeDetailDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(AppResponse<RecipeDetailDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AppResponse<RecipeDetailDto>>> GetRecipe(int id)
    {
        try
        {
            if (id <= 0)
            {
                var badRequestResponse = new AppResponse<RecipeDetailDto>()
                    .SetErrorResponse("Id", "Recipe ID must be greater than 0");
                return BadRequest(badRequestResponse);
            }

            var query = new GetRecipeByIdQuery(id);
            var result = await _mediator.Send(query);

            if (result.IsSucceeded)
            {
                return Ok(result);
            }

            if (result.Messages.ContainsKey("NotFound"))
            {
                return NotFound(result);
            }

            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetRecipe endpoint for ID {RecipeId}", id);
            var errorResponse = new AppResponse<RecipeDetailDto>()
                .SetErrorResponse("Error", "An unexpected error occurred while retrieving the recipe");
            return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
        }
    }

    /// <summary>
    /// Get available filter options for recipes (cuisines, meal types, etc.)
    /// </summary>
    /// <returns>Filter options</returns>
    [HttpGet("filter-options")]
    [ProducesResponseType(typeof(AppResponse<RecipeFilterOptionsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AppResponse<RecipeFilterOptionsDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AppResponse<RecipeFilterOptionsDto>>> GetFilterOptions()
    {
        try
        {
            var query = new GetRecipeFilterOptionsQuery();
            var result = await _mediator.Send(query);

            if (result.IsSucceeded)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetFilterOptions endpoint");
            var errorResponse = new AppResponse<RecipeFilterOptionsDto>()
                .SetErrorResponse("Error", "An unexpected error occurred while retrieving filter options");
            return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
        }
    }
} 