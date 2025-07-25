using DrHan.Application.DTOs.MealPlans;
using DrHan.Domain.Entities.Recipes;

namespace DrHan.Application.Services.SmartScoringService;

public interface ISmartScoringService
{
    /// <summary>
    /// Select the best recipe from filtered recipes using smart scoring
    /// </summary>
    Task<Recipe> SelectSmartRecipeAsync(List<Recipe> filteredRecipes, SmartSelectionContext context);
    
    /// <summary>
    /// Calculate smart score for a single recipe
    /// </summary>
    Task<RecipeScore> CalculateRecipeScoreAsync(Recipe recipe, SmartSelectionContext context);
    
    /// <summary>
    /// Get user's cuisine preferences based on historical data
    /// </summary>
    Task<List<UserCuisinePreference>> GetUserCuisinePreferencesAsync(int userId);
    
    /// <summary>
    /// Get recipes recently used by user for variety control
    /// </summary>
    Task<List<int>> GetRecentlyUsedRecipesAsync(int userId, int daysBack = 14);
    
    /// <summary>
    /// Get user's recipe completion rates for preference learning
    /// </summary>
    Task<Dictionary<int, double>> GetUserRecipeCompletionRatesAsync(int userId);
} 