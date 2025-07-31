using DrHan.Application.Commons;
using DrHan.Application.DTOs.Recipes;

namespace DrHan.Application.Interfaces.Services;

public interface IRecommendNewService
{
    /// <summary>
    /// Get recipe recommendations based on user's past meal plans, current time, and allergen exclusions
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="count">Number of recommendations to return (default: 10)</param>
    /// <returns>List of recommended recipes</returns>
    Task<AppResponse<List<RecipeDto>>> GetRecommendationsAsync(int userId, int count = 10);
    
    /// <summary>
    /// Get recipe recommendations for a specific meal type based on user's past preferences
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="mealType">Specific meal type (breakfast, lunch, dinner, snack)</param>
    /// <param name="count">Number of recommendations to return (default: 10)</param>
    /// <returns>List of recommended recipes</returns>
    Task<AppResponse<List<RecipeDto>>> GetRecommendationsByMealTypeAsync(int userId, string mealType, int count = 10);
} 