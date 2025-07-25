using DrHan.Application.Commons;
using DrHan.Application.Interfaces.Services;
using DrHan.Application.Interfaces.Services.AuthenticationServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DrHan.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Requires authentication
public class RecommendNewController : ControllerBase
{
    private readonly IRecommendNewService _recommendNewService;
    private readonly IUserContext _userContext;
    private readonly ILogger<RecommendNewController> _logger;

    public RecommendNewController(
        IRecommendNewService recommendNewService,
        IUserContext userContext,
        ILogger<RecommendNewController> logger)
    {
        _recommendNewService = recommendNewService;
        _userContext = userContext;
        _logger = logger;
    }

    /// <summary>
    /// 🍽️ Get personalized recipe recommendations based on your past meal plans and current time
    /// </summary>
    /// <param name="count">Number of recommendations to return (default: 10, max: 50)</param>
    /// <returns>List of recommended recipe IDs based on your preferences and current meal time</returns>
    [HttpGet("recommendations")]
    [ProducesResponseType(typeof(AppResponse<List<int>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AppResponse<List<int>>>> GetRecommendations([FromQuery] int count = 10)
    {
        try
        {
            // Validate count parameter
            if (count <= 0 || count > 50)
            {
                return BadRequest(new AppResponse<List<int>>()
                    .SetErrorResponse("InvalidCount", "Count must be between 1 and 50"));
            }

            var userId = _userContext.GetCurrentUserId().GetValueOrDefault();
            if (userId == 0)
            {
                return Unauthorized(new AppResponse<List<int>>()
                    .SetErrorResponse("Unauthorized", "User not authenticated"));
            }

            var result = await _recommendNewService.GetRecommendationsAsync(userId, count);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recommendations for user");
            var errorResponse = new AppResponse<List<int>>()
                .SetErrorResponse("Error", "An error occurred while getting recommendations");
            return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
        }
    }

    /// <summary>
    /// 🍽️ Get recipe recommendations for a specific meal type based on your past preferences
    /// </summary>
    /// <param name="mealType">Meal type (breakfast, lunch, dinner, snack)</param>
    /// <param name="count">Number of recommendations to return (default: 10, max: 50)</param>
    /// <returns>List of recommended recipe IDs for the specified meal type</returns>
    [HttpGet("recommendations/{mealType}")]
    [ProducesResponseType(typeof(AppResponse<List<int>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AppResponse<List<int>>>> GetRecommendationsByMealType(
        [FromRoute] string mealType,
        [FromQuery] int count = 10)
    {
        try
        {
            // Validate count parameter
            if (count <= 0 || count > 50)
            {
                return BadRequest(new AppResponse<List<int>>()
                    .SetErrorResponse("InvalidCount", "Count must be between 1 and 50"));
            }

            // Validate meal type
            var validMealTypes = new[] { "breakfast", "lunch", "dinner", "snack" };
            if (!validMealTypes.Contains(mealType.ToLower()))
            {
                return BadRequest(new AppResponse<List<int>>()
                    .SetErrorResponse("InvalidMealType", "Meal type must be one of: breakfast, lunch, dinner, snack"));
            }

            var userId = _userContext.GetCurrentUserId().GetValueOrDefault();
            if (userId == 0)
            {
                return Unauthorized(new AppResponse<List<int>>()
                    .SetErrorResponse("Unauthorized", "User not authenticated"));
            }

            var result = await _recommendNewService.GetRecommendationsByMealTypeAsync(userId, mealType, count);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting {MealType} recommendations for user", mealType);
            var errorResponse = new AppResponse<List<int>>()
                .SetErrorResponse("Error", "An error occurred while getting recommendations");
            return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
        }
    }

    /// <summary>
    /// 🕐 Get current meal type based on time of day
    /// </summary>
    /// <returns>Current meal type suggestion</returns>
    [HttpGet("current-meal-type")]
    [ProducesResponseType(typeof(AppResponse<string>), StatusCodes.Status200OK)]
    public ActionResult<AppResponse<string>> GetCurrentMealType()
    {
        try
        {
            var currentHour = DateTime.Now.Hour;
            var mealType = currentHour switch
            {
                >= 5 and < 11 => "breakfast",  // 5 AM - 10:59 AM
                >= 11 and < 16 => "lunch",     // 11 AM - 3:59 PM
                >= 16 and < 21 => "dinner",    // 4 PM - 8:59 PM
                _ => "snack"                   // 9 PM - 4:59 AM
            };

            var response = new AppResponse<string>();
            return Ok(response.SetSuccessResponse(mealType, "Success", $"Current meal type: {mealType}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current meal type");
            var errorResponse = new AppResponse<string>()
                .SetErrorResponse("Error", "An error occurred while getting current meal type");
            return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
        }
    }

    /// <summary>
    /// 🕐 Get current meal type based on time of day
    /// </summary>
    /// <returns>Current meal type suggestion</returns>
    private string GetMealTypeByCurrentTime()
    {
        var currentHour = DateTime.Now.Hour;
        return currentHour switch
        {
            >= 5 and < 11 => "breakfast",  // 5 AM - 10:59 AM
            >= 11 and < 16 => "lunch",     // 11 AM - 3:59 PM
            >= 16 and < 21 => "dinner",    // 4 PM - 8:59 PM
            _ => "snack"                   // 9 PM - 4:59 AM
        };
    }
} 