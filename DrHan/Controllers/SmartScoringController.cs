using DrHan.Application.Commons;
using DrHan.Application.DTOs.MealPlans;
using DrHan.Application.Interfaces.Services.AuthenticationServices;
using DrHan.Application.Services.SmartScoringService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DrHan.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Requires authentication
public class SmartScoringController : ControllerBase
{
    private readonly ISmartScoringService _smartScoringService;
    private readonly IUserContext _userContext;
    private readonly ILogger<SmartScoringController> _logger;

    public SmartScoringController(
        ISmartScoringService smartScoringService,
        IUserContext userContext,
        ILogger<SmartScoringController> logger)
    {
        _smartScoringService = smartScoringService;
        _userContext = userContext;
        _logger = logger;
    }

    /// <summary>
    /// 📊 Get user's cuisine preferences based on historical data
    /// </summary>
    /// <returns>List of user's cuisine preferences with usage statistics</returns>
    [HttpGet("user-cuisine-preferences")]
    [ProducesResponseType(typeof(AppResponse<List<UserCuisinePreference>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<AppResponse<List<UserCuisinePreference>>>> GetUserCuisinePreferences()
    {
        var response = new AppResponse<List<UserCuisinePreference>>();

        try
        {
            var userId = _userContext.GetCurrentUserId().GetValueOrDefault();
            var preferences = await _smartScoringService.GetUserCuisinePreferencesAsync(userId);
            
            return response.SetSuccessResponse(preferences, "Success", 
                $"Retrieved {preferences.Count} cuisine preferences for user");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user cuisine preferences");
            return response.SetErrorResponse("Error", "Failed to retrieve cuisine preferences");
        }
    }

    /// <summary>
    /// 🔄 Get recently used recipes for variety analysis
    /// </summary>
    /// <param name="daysBack">Number of days to look back (default: 14)</param>
    /// <returns>List of recently used recipe IDs</returns>
    [HttpGet("recently-used-recipes")]
    [ProducesResponseType(typeof(AppResponse<List<int>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<AppResponse<List<int>>>> GetRecentlyUsedRecipes([FromQuery] int daysBack = 14)
    {
        var response = new AppResponse<List<int>>();

        try
        {
            var userId = _userContext.GetCurrentUserId().GetValueOrDefault();
            var recentRecipes = await _smartScoringService.GetRecentlyUsedRecipesAsync(userId, daysBack);
            
            return response.SetSuccessResponse(recentRecipes, "Success", 
                $"Found {recentRecipes.Count} recently used recipes in last {daysBack} days");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recently used recipes");
            return response.SetErrorResponse("Error", "Failed to retrieve recently used recipes");
        }
    }

    /// <summary>
    /// ✅ Get user's recipe completion rates for preference learning
    /// </summary>
    /// <returns>Dictionary of recipe IDs and their completion rates</returns>
    [HttpGet("recipe-completion-rates")]
    [ProducesResponseType(typeof(AppResponse<Dictionary<int, double>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<AppResponse<Dictionary<int, double>>>> GetRecipeCompletionRates()
    {
        var response = new AppResponse<Dictionary<int, double>>();

        try
        {
            var userId = _userContext.GetCurrentUserId().GetValueOrDefault();
            var completionRates = await _smartScoringService.GetUserRecipeCompletionRatesAsync(userId);
            
            return response.SetSuccessResponse(completionRates, "Success", 
                $"Retrieved completion rates for {completionRates.Count} recipes");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recipe completion rates");
            return response.SetErrorResponse("Error", "Failed to retrieve recipe completion rates");
        }
    }
} 