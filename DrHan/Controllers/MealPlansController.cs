using DrHan.Application.Commons;
using DrHan.Application.DTOs.MealPlans;
using DrHan.Application.Services.MealPlanServices.Commands.CreateMealPlan;
using DrHan.Application.Services.MealPlanServices.Commands.GenerateSmartMealPlan;
using DrHan.Application.Services.MealPlanServices.Commands.GenerateSmartMeals;
using DrHan.Application.Services.MealPlanServices.Commands.AddMealEntry;
using DrHan.Application.Services.MealPlanServices.Commands.UpdateMealPlan;
using DrHan.Application.Services.MealPlanServices.Commands.DeleteMealPlan;
using DrHan.Application.Services.MealPlanServices.Queries.GetUserMealPlans;
using DrHan.Application.Services.MealPlanServices.Queries.GetMealPlanById;
using DrHan.Application.Interfaces.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DrHan.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Requires authentication
public class MealPlansController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ISmartMealPlanService _smartMealPlanService;
    private readonly ILogger<MealPlansController> _logger;

    public MealPlansController(
        IMediator mediator,
        ISmartMealPlanService smartMealPlanService,
        ILogger<MealPlansController> logger)
    {
        _mediator = mediator;
        _smartMealPlanService = smartMealPlanService;
        _logger = logger;
    }

    #region Smart Generation Endpoints

    /// <summary>
    /// 🎯 Generate a smart meal plan with AI recommendations
    /// </summary>
    /// <param name="request">Smart generation preferences</param>
    /// <returns>Complete meal plan with auto-selected meals</returns>
    [HttpPost("generate-smart")]
    [ProducesResponseType(typeof(AppResponse<MealPlanDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(AppResponse<MealPlanDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AppResponse<MealPlanDto>>> GenerateSmartMealPlan([FromBody] GenerateMealPlanDto request)
    {
        try
        {
            var command = new GenerateSmartMealPlanCommand(request);
            var result = await _mediator.Send(command);
            
            if (result.IsSucceeded)
            {
                _logger.LogInformation("Smart meal plan generated successfully");
                return CreatedAtAction(nameof(GetMealPlan), new { id = result.Data.Id }, result);
            }
            
            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GenerateSmartMealPlan endpoint");
            var errorResponse = new AppResponse<MealPlanDto>()
                .SetErrorResponse("Error", "An unexpected error occurred while generating the meal plan");
            return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
        }
    }

    /// <summary>
    /// 🔍 Get recipe recommendations based on preferences
    /// </summary>
    /// <param name="preferences">User preferences for filtering</param>
    /// <param name="mealType">Type of meal (breakfast, lunch, dinner, snack)</param>
    /// <returns>List of recommended recipe IDs</returns>
    [HttpPost("recommendations")]
    [ProducesResponseType(typeof(AppResponse<List<int>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<AppResponse<List<int>>>> GetRecommendations(
        [FromBody] MealPlanPreferencesDto preferences,
        [FromQuery] string mealType = "dinner")
    {
        try
        {
            var userId = User.Identity.Name; // Assuming this gets user ID
            var result = await _smartMealPlanService.GetRecommendedRecipesAsync(preferences, int.Parse(userId), mealType);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recipe recommendations");
            var errorResponse = new AppResponse<List<int>>()
                .SetErrorResponse("Error", "An error occurred while getting recommendations");
            return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
        }
    }

    /// <summary>
    /// 📋 Get available meal plan templates
    /// </summary>
    /// <returns>List of available templates</returns>
    [HttpGet("templates")]
    [ProducesResponseType(typeof(AppResponse<List<MealPlanTemplateDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<AppResponse<List<MealPlanTemplateDto>>>> GetTemplates()
    {
        var result = await _smartMealPlanService.GetAvailableTemplatesAsync();
        return Ok(result);
    }

    /// <summary>
    /// ⚙️ Get available options for smart meal generation
    /// </summary>
    /// <returns>All available options users can pick for smart meal/meal plan generation</returns>
    [HttpGet("smart-generation/options")]
    [ProducesResponseType(typeof(AppResponse<SmartGenerationOptionsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<AppResponse<SmartGenerationOptionsDto>>> GetSmartGenerationOptions()
    {
        try
        {
            var result = await _smartMealPlanService.GetSmartGenerationOptionsAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting smart generation options");
            var errorResponse = new AppResponse<SmartGenerationOptionsDto>()
                .SetErrorResponse("Error", "An error occurred while retrieving smart generation options");
            return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
        }
    }

    /// <summary>
    /// ⚡ Bulk fill meal slots with selected recipes
    /// </summary>
    /// <param name="request">Bulk fill configuration</param>
    /// <returns>Success confirmation</returns>
    [HttpPost("bulk-fill")]
    [ProducesResponseType(typeof(AppResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<AppResponse<bool>>> BulkFillMeals([FromBody] BulkFillMealsDto request)
    {
        try
        {
            var userId = User.Identity.Name;
            var result = await _smartMealPlanService.BulkFillMealsAsync(request, int.Parse(userId));
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk filling meals");
            var errorResponse = new AppResponse<bool>()
                .SetErrorResponse("Error", "An error occurred while bulk filling meals");
            return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
        }
    }

    #endregion

    #region Manual Meal Plan Management

    /// <summary>
    /// 📝 Create a new empty meal plan (manual approach)
    /// </summary>
    /// <param name="request">Meal plan basic information</param>
    /// <returns>Created meal plan</returns>
    [HttpPost]
    [ProducesResponseType(typeof(AppResponse<MealPlanDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(AppResponse<MealPlanDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AppResponse<MealPlanDto>>> CreateMealPlan([FromBody] CreateMealPlanDto request)
    {
        try
        {
            var command = new CreateMealPlanCommand(request);
            var result = await _mediator.Send(command);
            
            if (result.IsSucceeded)
                return CreatedAtAction(nameof(GetMealPlan), new { id = result.Data.Id }, result);
            
            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating meal plan");
            var errorResponse = new AppResponse<MealPlanDto>()
                .SetErrorResponse("Error", "An unexpected error occurred while creating the meal plan");
            return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
        }
    }

    /// <summary>
    /// ➕ Add a meal entry to existing meal plan (overrides existing entry for same day and meal type)
    /// </summary>
    /// <param name="mealPlanId">Meal plan ID</param>
    /// <param name="request">Meal entry details</param>
    /// <returns>Created or updated meal entry</returns>
    [HttpPost("{mealPlanId}/entries")]
    [ProducesResponseType(typeof(AppResponse<MealEntryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AppResponse<MealEntryDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AppResponse<MealEntryDto>>> AddMealEntry(
        int mealPlanId, 
        [FromBody] AddMealEntryDto request)
    {
        try
        {
            request.MealPlanId = mealPlanId;
            var command = new AddMealEntryCommand(request);
            var result = await _mediator.Send(command);
            
            return result.IsSucceeded ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding meal entry to plan {MealPlanId}", mealPlanId);
            var errorResponse = new AppResponse<MealEntryDto>()
                .SetErrorResponse("Error", "An unexpected error occurred while adding the meal entry");
            return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
        }
    }

    /// <summary>
    /// 🎯 Generate smart meals into existing meal plan
    /// </summary>
    /// <param name="mealPlanId">Meal plan ID</param>
    /// <param name="request">Smart generation preferences</param>
    /// <returns>Updated meal plan with generated meals</returns>
    [HttpPost("{mealPlanId}/generate-smart-meals")]
    [ProducesResponseType(typeof(AppResponse<MealPlanDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AppResponse<MealPlanDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(AppResponse<MealPlanDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AppResponse<MealPlanDto>>> GenerateSmartMeals(
        int mealPlanId,
        [FromBody] GenerateSmartMealsDto request)
    {
        try
        {
            var command = new GenerateSmartMealsCommand(mealPlanId, request);
            var result = await _mediator.Send(command);
            
            if (result.IsSucceeded)
            {
                _logger.LogInformation("Smart meals generated successfully for meal plan {MealPlanId}", mealPlanId);
                return Ok(result);
            }
            
            if (result.Messages?.ContainsKey("NotFound") == true)
                return NotFound(result);
                
            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating smart meals for meal plan {MealPlanId}", mealPlanId);
            var errorResponse = new AppResponse<MealPlanDto>()
                .SetErrorResponse("Error", "An unexpected error occurred while generating smart meals");
            return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
        }
    }

    #endregion

    #region Query Endpoints

    /// <summary>
    /// 📄 Get user's meal plans with pagination
    /// </summary>
    /// <param name="pagination">Pagination parameters</param>
    /// <returns>Paginated list of meal plans</returns>
    [HttpGet]
    [ProducesResponseType(typeof(AppResponse<PaginatedList<MealPlanDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<AppResponse<PaginatedList<MealPlanDto>>>> GetMealPlans(
        [FromQuery] PaginationRequest pagination)
    {
        try
        {
            var query = new GetUserMealPlansQuery(pagination);
            var result = await _mediator.Send(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving meal plans");
            var errorResponse = new AppResponse<PaginatedList<MealPlanDto>>()
                .SetErrorResponse("Error", "An error occurred while retrieving meal plans");
            return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
        }
    }

    /// <summary>
    /// 🔍 Get a specific meal plan by ID
    /// </summary>
    /// <param name="id">Meal plan ID</param>
    /// <returns>Detailed meal plan with all entries</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(AppResponse<MealPlanDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AppResponse<MealPlanDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AppResponse<MealPlanDto>>> GetMealPlan(int id)
    {
        try
        {
            if (id <= 0)
            {
                var badRequestResponse = new AppResponse<MealPlanDto>()
                    .SetErrorResponse("Id", "Meal plan ID must be greater than 0");
                return BadRequest(badRequestResponse);
            }

            var query = new GetMealPlanByIdQuery(id);
            var result = await _mediator.Send(query);
            
            if (result.IsSucceeded)
                return Ok(result);
            
            return result.Messages.ContainsKey("NotFound") ? NotFound(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving meal plan {MealPlanId}", id);
            var errorResponse = new AppResponse<MealPlanDto>()
                .SetErrorResponse("Error", "An unexpected error occurred while retrieving the meal plan");
            return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
        }
    }

    #endregion

    #region Update & Delete Operations

    /// <summary>
    /// ✏️ Update meal plan details
    /// </summary>
    /// <param name="id">Meal plan ID</param>
    /// <param name="request">Updated meal plan data</param>
    /// <returns>Updated meal plan</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(AppResponse<MealPlanDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AppResponse<MealPlanDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AppResponse<MealPlanDto>>> UpdateMealPlan(
        int id, 
        [FromBody] UpdateMealPlanDto request)
    {
        try
        {
            request.Id = id;
            var command = new UpdateMealPlanCommand(request);
            var result = await _mediator.Send(command);
            
            return result.IsSucceeded ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating meal plan {MealPlanId}", id);
            var errorResponse = new AppResponse<MealPlanDto>()
                .SetErrorResponse("Error", "An unexpected error occurred while updating the meal plan");
            return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
        }
    }

    /// <summary>
    /// 🗑️ Delete meal plan
    /// </summary>
    /// <param name="id">Meal plan ID</param>
    /// <returns>Deletion confirmation</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(AppResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AppResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AppResponse<bool>>> DeleteMealPlan(int id)
    {
        try
        {
            var command = new DeleteMealPlanCommand(id);
            var result = await _mediator.Send(command);
            
            return result.IsSucceeded ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting meal plan {MealPlanId}", id);
            var errorResponse = new AppResponse<bool>()
                .SetErrorResponse("Error", "An unexpected error occurred while deleting the meal plan");
            return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
        }
    }

    #endregion
}