using DrHan.Application.Commons;
using DrHan.Application.DTOs.Recipes;
using DrHan.Application.Interfaces.Repository;
using DrHan.Application.Services.RecipeServices.Queries.GetRecipeById;
using DrHan.Application.Services.RecipeServices.Queries.SearchRecipes;
using DrHan.Domain.Entities.Ingredients;
using DrHan.Domain.Entities.Recipes;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DrHan.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RecipesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<RecipesController> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public RecipesController(IMediator mediator, ILogger<RecipesController> logger, IUnitOfWork unitOfWork)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
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
    public async Task<ActionResult<AppResponse<RecipeFilterOptionsDto>>> GetFilterOptions()
    {
        try
        {
            // Get dynamic filter options from database
            var recipes = await _unitOfWork.Repository<Recipe>().ListAsync(
                includeProperties: query => query
                    .Include(r => r.RecipeAllergens)
                    .Include(r => r.RecipeAllergenFreeClaims)
                    .Include(r => r.RecipeIngredients)
                        .ThenInclude(ri => ri.Ingredient)
            );

            // Get all ingredients from the database for comprehensive filtering
            var allIngredients = await _unitOfWork.Repository<Ingredient>().ListAsync();
            
            var filterOptions = new RecipeFilterOptionsDto
            {
                CuisineTypes = recipes
                    .Where(r => !string.IsNullOrEmpty(r.CuisineType))
                    .Select(r => r.CuisineType)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList(),
                    
                MealTypes = recipes
                    .Where(r => !string.IsNullOrEmpty(r.MealType))
                    .Select(r => r.MealType)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList(),
                    
                DifficultyLevels = recipes
                    .Where(r => !string.IsNullOrEmpty(r.DifficultyLevel))
                    .Select(r => r.DifficultyLevel)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList(),
                    
                SortOptions = new List<string>
                {
                    "Name", "Rating", "PrepTime", "Likes"
                },
                
                // Add available allergens from recipes
                AvailableAllergens = recipes
                    .SelectMany(r => r.RecipeAllergens)
                    .Where(ra => !string.IsNullOrEmpty(ra.AllergenType))
                    .Select(ra => ra.AllergenType)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList(),
                    
                // Add available allergen-free claims
                AvailableAllergenFreeClaims = recipes
                    .SelectMany(r => r.RecipeAllergenFreeClaims)
                    .Where(rc => !string.IsNullOrEmpty(rc.Claim))
                    .Select(rc => rc.Claim)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList(),

                // Add available ingredients from recipes
                AvailableIngredients = recipes
                    .SelectMany(r => r.RecipeIngredients)
                    .Where(ri => !string.IsNullOrEmpty(ri.IngredientName))
                    .Select(ri => ri.IngredientName)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList(),

                // Add ingredient categories
                IngredientCategories = allIngredients
                    .Where(i => !string.IsNullOrEmpty(i.Category))
                    .Select(i => i.Category!)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList()
            };

            var response = new AppResponse<RecipeFilterOptionsDto>()
                .SetSuccessResponse(filterOptions);

            return Ok(response);
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

public class RecipeFilterOptionsDto
{
    public List<string> CuisineTypes { get; set; } = new();
    public List<string> MealTypes { get; set; } = new();
    public List<string> DifficultyLevels { get; set; } = new();
    public List<string> SortOptions { get; set; } = new();
    public List<string> AvailableAllergens { get; set; } = new();
    public List<string> AvailableAllergenFreeClaims { get; set; } = new();
    public List<string> AvailableIngredients { get; set; } = new();
    public List<string> IngredientCategories { get; set; } = new();
} 