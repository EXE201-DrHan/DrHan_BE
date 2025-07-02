using DrHan.Application.DTOs.MealPlans;
using DrHan.Domain.Entities.Recipes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace DrHan.Application.StaticQuery;

public static class MealPlanRecipeQuery
{
    /// <summary>
    /// Build filter expression for recipes based on meal plan preferences and user allergies
    /// </summary>
    public static Expression<Func<Recipe, bool>> BuildMealPlanFilter(
        MealPlanPreferencesDto preferences, 
        List<int> userAllergies, 
        string mealType)
    {
        return recipe =>
            // Filter out recipes with user allergies
            (!userAllergies.Any() || 
             !recipe.RecipeAllergens.Any(ra => userAllergies.Contains(ra.AllergenId ?? 0))) &&

            // Filter by cooking time if specified
            (!preferences.MaxCookingTime.HasValue ||
             recipe.CookTimeMinutes <= preferences.MaxCookingTime.Value ||
             recipe.CookTimeMinutes == null) &&

            // Filter by cuisine type if specified
            (!preferences.CuisineTypes.Any() ||
             preferences.CuisineTypes.Contains(recipe.CuisineType)) &&

            // Simplified meal type filtering for EF Core compatibility
            (string.IsNullOrEmpty(mealType) ||
             recipe.MealType.Contains(mealType) ||
             recipe.Name.ToLower().Contains(mealType.ToLower()) ||
             recipe.Description.ToLower().Contains(mealType.ToLower()));
    }

    /// <summary>
    /// Build ordering for meal plan recipes (prioritize highly rated, appropriate timing)
    /// </summary>
    public static Func<IQueryable<Recipe>, IOrderedQueryable<Recipe>> BuildMealPlanOrderBy(string mealType)
    {
        return mealType.ToLower() switch
        {
            "breakfast" => query => query
                .OrderBy(r => r.PrepTimeMinutes ?? 999) // Quick breakfast first
                .ThenByDescending(r => r.RatingAverage)
                .ThenBy(r => r.Name),

            "lunch" => query => query
                .OrderBy(r => (r.PrepTimeMinutes ?? 0) + (r.CookTimeMinutes ?? 0)) // Total time
                .ThenByDescending(r => r.RatingAverage)
                .ThenBy(r => r.Name),

            "dinner" => query => query
                .OrderByDescending(r => r.RatingAverage) // Quality first for dinner
                .ThenBy(r => r.CookTimeMinutes ?? 999)
                .ThenBy(r => r.Name),

            "snack" => query => query
                .OrderBy(r => r.PrepTimeMinutes ?? 999) // Quick snacks first
                .ThenByDescending(r => r.RatingAverage)
                .ThenBy(r => r.Name),

            _ => query => query
                .OrderByDescending(r => r.RatingAverage)
                .ThenBy(r => r.Name)
        };
    }

    /// <summary>
    /// Essential includes for meal plan recipe queries
    /// </summary>
    public static Func<IQueryable<Recipe>, IIncludableQueryable<Recipe, object>> BuildMealPlanIncludes()
    {
        return query => query
            .Include(r => r.RecipeAllergens) // For allergy filtering
            .Include(r => r.RecipeAllergenFreeClaims) // For dietary restrictions
            .Include(r => r.RecipeNutritions) // For nutritional info
            .Include(r => r.RecipeImages.Take(1)); // Only first image for performance
    }

    /// <summary>
    /// Get recommended recipe count based on meal type and preferences
    /// </summary>
    public static int GetRecommendedRecipeCount(string mealType, MealPlanPreferencesDto preferences)
    {
        var baseCount = mealType.ToLower() switch
        {
            "breakfast" => 20, // More breakfast variety needed
            "lunch" => 30,     // Lunch needs variety for work days
            "dinner" => 40,    // Dinner is main meal, needs most variety
            "snack" => 15,     // Fewer snack options needed
            _ => 25
        };

        // Adjust based on preferences
        if (preferences.CuisineTypes.Count > 2) baseCount += 10; // More variety needed
        if (preferences.MaxCookingTime.HasValue && preferences.MaxCookingTime < 30) baseCount += 10; // Quick meals are limited

        return Math.Min(baseCount, 100); // Cap at 100 for performance
    }
} 