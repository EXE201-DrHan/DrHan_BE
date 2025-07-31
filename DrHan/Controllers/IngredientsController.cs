using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using DrHan.Application.Commons;
using DrHan.Application.DTOs.Ingredients;
using DrHan.Application.Services.IngredientServices.Commands.CreateIngredient;
using DrHan.Application.Services.IngredientServices.Commands.DeleteIngredient;
using DrHan.Application.Services.IngredientServices.Commands.UpdateIngredient;
using DrHan.Application.Services.IngredientServices.Commands.AddAllergenToIngredient;
using DrHan.Application.Services.IngredientServices.Queries.GetAllIngredients;
using DrHan.Application.Services.IngredientServices.Queries.GetIngredientById;
using DrHan.Application.Services.IngredientServices.Queries.GetIngredientCategories;

namespace DrHan.Controllers;

[Route("api/[controller]")]
[ApiController]
public class IngredientsController : ControllerBase
{
    private readonly IMediator _mediator;

    public IngredientsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all ingredients with pagination, search, and category filtering
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="size">Page size (default: 20)</param>
    /// <param name="search">Search term for ingredient name or description</param>
    /// <param name="category">Filter by category</param>
    /// <returns>Paginated list of ingredients</returns>
    [HttpGet]
    public async Task<ActionResult<AppResponse<IPaginatedList<IngredientDto>>>> GetAllIngredients(
        [FromQuery] int page = 1, 
        [FromQuery] int size = 20, 
        [FromQuery] string? search = null, 
        [FromQuery] string? category = null)
    {
        var query = new GetAllIngredientsQuery
        {
            Page = page,
            Size = size,
            Search = search,
            Category = category
        };
        
        var response = await _mediator.Send(query);
        return response.IsSucceeded ? Ok(response) : BadRequest(response);
    }

    /// <summary>
    /// Get ingredient by ID
    /// </summary>
    /// <param name="id">Ingredient ID</param>
    /// <returns>Ingredient details</returns>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<AppResponse<IngredientDto>>> GetIngredientById(int id)
    {
        var query = new GetIngredientByIdQuery { Id = id };
        var response = await _mediator.Send(query);
        return response.IsSucceeded ? Ok(response) : NotFound(response);
    }

    /// <summary>
    /// Get all ingredient categories with counts
    /// </summary>
    /// <returns>List of ingredient categories</returns>
    [HttpGet("categories")]
    public async Task<ActionResult<AppResponse<List<IngredientCategoryDto>>>> GetIngredientCategories()
    {
        var query = new GetIngredientCategoriesQuery();
        var response = await _mediator.Send(query);
        return response.IsSucceeded ? Ok(response) : BadRequest(response);
    }



    /// <summary>
    /// Create a new ingredient
    /// </summary>
    /// <param name="createDto">Ingredient creation data</param>
    /// <returns>Created ingredient</returns>
    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<AppResponse<IngredientDto>>> CreateIngredient([FromBody] CreateIngredientDto createDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var command = new CreateIngredientCommand
        {
            Name = createDto.Name,
            Category = createDto.Category,
            Description = createDto.Description,
            Nutritions = createDto.Nutritions,
            AlternativeNames = createDto.AlternativeNames,
            AllergenIds = createDto.AllergenIds
        };
        
        var response = await _mediator.Send(command);
        return response.IsSucceeded ? 
            CreatedAtAction(nameof(GetIngredientById), new { id = response.Data!.Id }, response) :
            BadRequest(response);
    }

    /// <summary>
    /// Update an existing ingredient
    /// </summary>
    /// <param name="id">Ingredient ID</param>
    /// <param name="updateDto">Ingredient update data</param>
    /// <returns>Updated ingredient</returns>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<AppResponse<IngredientDto>>> UpdateIngredient(
        int id, 
        [FromBody] UpdateIngredientDto updateDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var command = new UpdateIngredientCommand
        {
            Id = id,
            Name = updateDto.Name,
            Category = updateDto.Category,
            Description = updateDto.Description,
            Nutritions = updateDto.Nutritions,
            AlternativeNames = updateDto.AlternativeNames,
            AllergenIds = updateDto.AllergenIds
        };
        
        var response = await _mediator.Send(command);
        return response.IsSucceeded ? Ok(response) : BadRequest(response);
    }

    /// <summary>
    /// Add an allergen to an ingredient
    /// </summary>
    /// <param name="addAllergenDto">Data for adding allergen to ingredient</param>
    /// <returns>Created ingredient allergen relationship</returns>
    [HttpPost("add-allergen")]
    public async Task<ActionResult<AppResponse<IngredientAllergenDto>>> AddAllergenToIngredient([FromBody] AddAllergenToIngredientDto addAllergenDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var command = new AddAllergenToIngredientCommand
        {
            IngredientId = addAllergenDto.IngredientId,
            AllergenId = addAllergenDto.AllergenId,
            AllergenType = addAllergenDto.AllergenType
        };

        var response = await _mediator.Send(command);
        return response.IsSucceeded ? Ok(response) : BadRequest(response);
    }

    /// <summary>
    /// Delete an ingredient
    /// </summary>
    /// <param name="id">Ingredient ID</param>
    /// <returns>Deletion confirmation</returns>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<AppResponse<bool>>> DeleteIngredient(int id)
    {
        var command = new DeleteIngredientCommand(id);
        var response = await _mediator.Send(command);
        return response.IsSucceeded ? Ok(response) : BadRequest(response);
    }

    /// <summary>
    /// Clear ingredient cache (Admin only)
    /// </summary>
    /// <returns>Cache clear confirmation</returns>
    [HttpPost("clear-cache")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> ClearCache()
    {
        // TODO: Implement cache clearing command
        return Ok(new { Message = "Ingredient cache cleared successfully" });
    }

    /// <summary>
    /// Get ingredient statistics
    /// </summary>
    /// <returns>Ingredient statistics</returns>
    [HttpGet("statistics")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult> GetIngredientStatistics()
    {
        var categoriesResponse = await _mediator.Send(new GetIngredientCategoriesQuery());
        
        if (!categoriesResponse.IsSucceeded)
            return BadRequest(categoriesResponse);

        var totalIngredients = categoriesResponse.Data!.Sum(c => c.Count);
        var totalCategories = categoriesResponse.Data!.Count;
        var topCategories = categoriesResponse.Data!
            .OrderByDescending(c => c.Count)
            .Take(5)
            .ToList();

        return Ok(new
        {
            TotalIngredients = totalIngredients,
            TotalCategories = totalCategories,
            TopCategories = topCategories,
            CategoriesBreakdown = categoriesResponse.Data
        });
    }

    /// <summary>
    /// Bulk create ingredients
    /// </summary>
    /// <param name="ingredients">List of ingredients to create</param>
    /// <returns>Creation results</returns>
    [HttpPost("bulk")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> BulkCreateIngredients([FromBody] List<CreateIngredientDto> ingredients)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var results = new List<object>();
        var successCount = 0;
        var errorCount = 0;

        foreach (var ingredient in ingredients)
        {
            try
            {
                var response = await _mediator.Send(new CreateIngredientCommand
                {
                    Name = ingredient.Name,
                    Category = ingredient.Category,
                    Description = ingredient.Description,
                    Nutritions = ingredient.Nutritions,
                    AlternativeNames = ingredient.AlternativeNames,
                    AllergenIds = ingredient.AllergenIds
                });
                if (response.IsSucceeded)
                {
                    successCount++;
                    results.Add(new { 
                        Name = ingredient.Name, 
                        Status = "Success", 
                        Id = response.Data!.Id 
                    });
                }
                else
                {
                    errorCount++;
                    results.Add(new { 
                        Name = ingredient.Name, 
                        Status = "Error", 
                        Message = "Failed to create ingredient" 
                    });
                }
            }
            catch (Exception ex)
            {
                errorCount++;
                results.Add(new { 
                    Name = ingredient.Name, 
                    Status = "Error", 
                    Message = ex.Message 
                });
            }
        }

        return Ok(new
        {
            TotalProcessed = ingredients.Count,
            SuccessCount = successCount,
            ErrorCount = errorCount,
            Results = results
        });
    }
} 