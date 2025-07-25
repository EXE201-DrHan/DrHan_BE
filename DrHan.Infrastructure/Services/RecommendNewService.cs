using DrHan.Application.Commons;
using DrHan.Application.Interfaces.Repository;
using DrHan.Application.Interfaces.Services;
using DrHan.Domain.Constants;
using DrHan.Domain.Entities.MealPlans;
using DrHan.Domain.Entities.Recipes;
using DrHan.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Linq.Expressions;

namespace DrHan.Infrastructure.Services;

public class RecommendNewService : IRecommendNewService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RecommendNewService> _logger;
    private readonly Random _random = new Random();

    public RecommendNewService(
        IUnitOfWork unitOfWork,
        ILogger<RecommendNewService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<AppResponse<List<int>>> GetRecommendationsAsync(int userId, int count = 10)
    {
        var response = new AppResponse<List<int>>();

        try
        {
            // Determine meal type based on current time
            var currentMealType = GetMealTypeByCurrentTime();
            
            _logger.LogInformation("Getting recommendations for user {UserId} at meal type {MealType}", userId, currentMealType);

            // Get user allergies
            var userAllergies = await GetUserAllergiesAsync(userId);
            
            // Get user's cuisine preferences from past meal plans
            var userPreferences = await GetUserCuisinePreferencesAsync(userId);
            
            // Get recommendations based on past preferences and current meal type
            var recommendations = await GetRecommendationsBasedOnHistoryAsync(userId, currentMealType, userAllergies, userPreferences, count);

            if (!recommendations.Any())
            {
                return response.SetErrorResponse("NoRecommendations", "No suitable recommendations found based on your preferences");
            }

            return response.SetSuccessResponse(recommendations, "Success", $"Found {recommendations.Count} recommendations for {currentMealType}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recommendations for user {UserId}", userId);
            return response.SetErrorResponse("Error", "Failed to get recommendations");
        }
    }

    public async Task<AppResponse<List<int>>> GetRecommendationsByMealTypeAsync(int userId, string mealType, int count = 10)
    {
        var response = new AppResponse<List<int>>();

        try
        {
            _logger.LogInformation("Getting {MealType} recommendations for user {UserId}", mealType, userId);

            // Get user allergies
            var userAllergies = await GetUserAllergiesAsync(userId);
            
            // Get user's cuisine preferences from past meal plans
            var userPreferences = await GetUserCuisinePreferencesAsync(userId);
            
            // Get recommendations for specific meal type
            var recommendations = await GetRecommendationsBasedOnHistoryAsync(userId, mealType, userAllergies, userPreferences, count);

            if (!recommendations.Any())
            {
                return response.SetErrorResponse("NoRecommendations", $"No suitable {mealType} recommendations found based on your preferences");
            }

            return response.SetSuccessResponse(recommendations, "Success", $"Found {recommendations.Count} {mealType} recommendations");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting {MealType} recommendations for user {UserId}", mealType, userId);
            return response.SetErrorResponse("Error", "Failed to get recommendations");
        }
    }

    private string GetMealTypeByCurrentTime()
    {
        var currentHour = DateTime.Now.Hour;
        
        return currentHour switch
        {
            >= 5 and < 11 => MealTypeConstants.BREAKFAST,  // 5 AM - 10:59 AM
            >= 11 and < 16 => MealTypeConstants.LUNCH,     // 11 AM - 3:59 PM
            >= 16 and < 21 => MealTypeConstants.DINNER,    // 4 PM - 8:59 PM
            _ => MealTypeConstants.SNACK                    // 9 PM - 4:59 AM
        };
    }

    private async Task<List<int>> GetUserAllergiesAsync(int userId)
    {
        try
        {
            var userAllergies = await _unitOfWork.Repository<UserAllergy>()
                .ListAsync(
                    filter: ua => ua.UserId == userId,
                    includeProperties: query => query
                        .Include(ua => ua.Allergen)
                        .Include(ua => ua.ApplicationUser)
                );
            
            var result = userAllergies
                .Where(ua => ua.AllergenId.HasValue)
                .Select(ua => ua.AllergenId.Value)
                .ToList();

            var allergyNames = userAllergies
                .Where(ua => ua.AllergenId.HasValue && ua.Allergen != null)
                .Select(ua => ua.Allergen.Name)
                .ToList();

            _logger.LogInformation("Found {Count} allergies for user {UserId}: {Allergies} ({AllergyNames})", 
                result.Count, userId, string.Join(", ", result), string.Join(", ", allergyNames));

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving allergies for user {UserId}", userId);
            return new List<int>();
        }
    }

    private async Task<UserCuisinePreferences> GetUserCuisinePreferencesAsync(int userId)
    {
        try
        {
            // Get user's meal plan entries from the last 3 months
            var cutoffDate = DateOnly.FromDateTime(DateTime.Now.AddMonths(-3));
            
            var mealEntries = await _unitOfWork.Repository<MealPlanEntry>()
                .ListAsync(
                    filter: mpe => mpe.MealDate >= cutoffDate && mpe.RecipeId.HasValue,
                    includeProperties: query => query
                        .Include(mpe => mpe.MealPlan)
                        .Include(mpe => mpe.Recipe)
                        .ThenInclude(r => r.RecipeIngredients)
                        .ThenInclude(ri => ri.Ingredient)
                        .ThenInclude(i => i.IngredientAllergens)
                        .ThenInclude(ia => ia.Allergen)
                );

            // Filter by user and analyze preferences
            var userMealEntries = mealEntries
                .Where(mpe => mpe.MealPlan != null && mpe.MealPlan.UserId == userId)
                .ToList();

            _logger.LogInformation("Found {Count} meal entries for user {UserId} from {CutoffDate}", 
                userMealEntries.Count, userId, cutoffDate);

            var preferences = AnalyzeUserPreferences(userMealEntries);

            _logger.LogInformation("User {UserId} preferences - Cuisines: {CuisineCount}, MealTypes: {MealTypeCount}, Recent: {RecentCount}, Favorites: {FavoriteCount}", 
                userId, 
                preferences.CuisinePreferences.Count, 
                preferences.MealTypePreferences.Count,
                preferences.RecentlyUsedRecipeIds.Count,
                preferences.FavoriteRecipeIds.Count);

            return preferences;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cuisine preferences for user {UserId}", userId);
            return new UserCuisinePreferences();
        }
    }

    private UserCuisinePreferences AnalyzeUserPreferences(List<MealPlanEntry> userMealEntries)
    {
        var preferences = new UserCuisinePreferences();

        if (!userMealEntries.Any())
        {
            return preferences;
        }

        // Analyze cuisine preferences
        var cuisineStats = userMealEntries
            .Where(mpe => mpe.Recipe != null && !string.IsNullOrEmpty(mpe.Recipe.CuisineType))
            .GroupBy(mpe => mpe.Recipe.CuisineType)
            .Select(group => new CuisinePreference
            {
                CuisineType = group.Key,
                UsageCount = group.Count(),
                PreferenceRatio = (double)group.Count() / userMealEntries.Count,
                CompletionRate = group.Count(mpe => mpe.IsCompleted) / (double)group.Count()
            })
            .OrderByDescending(cp => cp.UsageCount)
            .ToList();

        preferences.CuisinePreferences = cuisineStats;

        // Analyze meal type preferences
        var mealTypeStats = userMealEntries
            .Where(mpe => mpe.Recipe != null && !string.IsNullOrEmpty(mpe.Recipe.MealType))
            .GroupBy(mpe => mpe.Recipe.MealType)
            .Select(group => new MealTypePreference
            {
                MealType = group.Key,
                UsageCount = group.Count(),
                PreferenceRatio = (double)group.Count() / userMealEntries.Count
            })
            .OrderByDescending(mp => mp.UsageCount)
            .ToList();

        preferences.MealTypePreferences = mealTypeStats;

        // Get recently used recipes (last 2 weeks)
        var cutoffDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-14));
        preferences.RecentlyUsedRecipeIds = userMealEntries
            .Where(mpe => mpe.MealDate >= cutoffDate)
            .Select(mpe => mpe.RecipeId.Value)
            .Distinct()
            .ToList();

        // Get favorite recipes (high completion rate)
        preferences.FavoriteRecipeIds = userMealEntries
            .GroupBy(mpe => mpe.RecipeId.Value)
            .Where(group => group.Count() >= 2 && group.Count(mpe => mpe.IsCompleted) / (double)group.Count() >= 0.8)
            .Select(group => group.Key)
            .ToList();

        return preferences;
    }

    private async Task<List<int>> GetRecommendationsBasedOnHistoryAsync(
        int userId, 
        string mealType, 
        List<int> userAllergies, 
        UserCuisinePreferences userPreferences, 
        int count)
    {
        try
        {
            _logger.LogInformation("Getting recommendations for user {UserId}, mealType: {MealType}, allergies: {Allergies}, count: {Count}", 
                userId, mealType, string.Join(", ", userAllergies), count);
            var recipes = await _unitOfWork.Repository<Recipe>()
                .ListAsync(
                includeProperties: query => query
                        .Include(r => r.RecipeAllergens)
                        .Include(r => r.RecipeNutritions)
                        .Include(r => r.RecipeImages)
                        .Include(r => r.RecipeIngredients)
                        .ThenInclude(ri => ri.Ingredient)
                        .ThenInclude(i => i.IngredientAllergens)
                        .ThenInclude(ia => ia.Allergen)
                        .Include(r => r.RecipeInstructions)
                );
            
            var recipes1 = recipes.Where(c => IsRecipeAllergySafe(c,userAllergies));
            _logger.LogInformation("Found {RecipeCount} recipes after filtering for mealType: {MealType}", 
                recipes.Count, mealType);

            if (!recipes.Any())
            {
                _logger.LogWarning("No recipes found for user {UserId} with parameters: mealType={MealType}, allergies={Allergies}", 
                    userId, mealType, string.Join(", ", userAllergies));
                return new List<int>();
            }

            // Log detailed allergen information for debugging
            foreach (var recipe1 in recipes1.Take(3)) // Log first 3 recipes for debugging
            {
                var recipeAllergens = recipe1.RecipeAllergens?.Select(ra => ra.Allergen?.Name).Where(n => !string.IsNullOrEmpty(n)) ?? new List<string>();
                var ingredientAllergens = recipe1.RecipeIngredients?
                    .Where(ri => ri.Ingredient != null)
                    .SelectMany(ri => ri.Ingredient.IngredientAllergens)
                    .Select(ia => ia.Allergen?.Name)
                    .Where(n => !string.IsNullOrEmpty(n))
                    .Distinct() ?? new List<string>();
                
                _logger.LogDebug("Recipe {RecipeId} '{RecipeName}' allergens - Direct: {DirectAllergens}, From Ingredients: {IngredientAllergens}", 
                    recipe1.Id, recipe1.Name, 
                    string.Join(", ", recipeAllergens), 
                    string.Join(", ", ingredientAllergens));
            }
            // Score and rank recipes based on user preferences
            var scoredRecipes = recipes1
    .Select(recipe => new
    {
        Recipe = recipe,
        Score = CalculateRecommendationScore(recipe, userPreferences, mealType)
    })
    .OrderByDescending(sr => sr.Score) // Order by score descending
    .OrderBy(_ => Random.Shared.Next()) // Shuffle the sorted list
    .Take(count) // Take the top 'count' recipes
    .Select(sr => sr.Recipe.Id) // Select recipe IDs
    .ToList();

            _logger.LogInformation("Returning {Count} recommendations for user {UserId}, mealType: {MealType}", 
                scoredRecipes.Count, userId, mealType);

            return scoredRecipes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recommendations based on history for user {UserId}", userId);
            return new List<int>();
        }
    }
    public List<int> MixUpRecommendations(List<Recipe> candidateRecipes)
    {
        var random = new Random();

        // Group recipes by quality/score tiers
        var highQuality = candidateRecipes.Where(r => r.RecipeIngredients.Count() >= 4.0).ToList();
        var mediumQuality = candidateRecipes.Where(r => r.RecipeIngredients.Count() >= 3.0 && r.RecipeIngredients.Count() < 4.0).ToList();
        var basicQuality = candidateRecipes.Where(r => r.RecipeIngredients.Count() < 3.0).ToList();

        var recommendations = new List<Recipe>();

        // Mix different quality tiers
        for (int i = 0; i < candidateRecipes.Count(); i++)
        {
            List<Recipe> selectedTier;
            var rand = random.NextDouble();

            // 60% high quality, 30% medium, 10% basic
            if (rand < 0.6 && highQuality.Any())
                selectedTier = highQuality;
            else if (rand < 0.9 && mediumQuality.Any())
                selectedTier = mediumQuality;
            else
                selectedTier = basicQuality.Any() ? basicQuality : candidateRecipes;

            if (selectedTier.Any())
            {
                var selected = selectedTier[random.Next(selectedTier.Count)];
                recommendations.Add(selected);

                // Remove from all tiers to avoid duplicates
                highQuality.Remove(selected);
                mediumQuality.Remove(selected);
                basicQuality.Remove(selected);
                candidateRecipes.Remove(selected);
            }
        }

        return recommendations.Select(r => r.Id).ToList();
    }
    private bool IsRecipeAllergySafe(Recipe recipe, List<int> userAllergies)
    {
        // Check recipe-level allergens
        if (userAllergies == null || !userAllergies.Any())
            return true;

        if (recipe.RecipeAllergens?.Any() == true)
        {
            var recipeAllergens = recipe.RecipeAllergens
                .Where(ra => ra.AllergenId.HasValue) // Filter out nulls
                .Select(ra => ra.AllergenId.Value);  // Safe to use .Value now

            if (recipeAllergens.Any(allergen => userAllergies.Contains(allergen)))
                return false;
        }
        if (recipe.RecipeIngredients?.Any() == true)
        {
            foreach (var recipeIngredient in recipe.RecipeIngredients)
            {
                if (recipeIngredient?.Ingredient?.IngredientAllergens?.Any() == true)
                {
                    var ingredientAllergens = recipeIngredient.Ingredient.IngredientAllergens
                        .Where(ia => ia.AllergenId.HasValue)
                        .Select(ia => ia.AllergenId.Value);

                    var matchingAllergens = ingredientAllergens.Where(a => userAllergies.Contains(a)).ToList();
                    if (matchingAllergens.Any())
                    {
                        _logger.LogInformation("Recipe {RecipeId} rejected: ingredient '{IngredientName}' contains allergens {Allergens}",
                            recipe.Id, recipeIngredient.Ingredient.Name, string.Join(", ", matchingAllergens));
                        return false;
                    }
                }
            }
        }
        return true;
    }

    private double CalculateRecommendationScore(Recipe recipe, UserCuisinePreferences preferences, string mealType)
    {
        var score = 0.0;

        // Base score from recipe rating (0-5 scale)
        var rating = (double)(recipe.RatingAverage ?? 0);
        score += rating * 0.3; // 30% weight for rating

        // Cuisine preference score (25% weight)
        if (!string.IsNullOrEmpty(recipe.CuisineType))
        {
            var cuisinePreference = preferences.CuisinePreferences
                .FirstOrDefault(cp => cp.CuisineType == recipe.CuisineType);
            
            if (cuisinePreference != null)
            {
                score += cuisinePreference.PreferenceRatio * 0.25;
            }
        }

        // Meal type preference score (20% weight)
        if (!string.IsNullOrEmpty(recipe.MealType))
        {
            var mealTypePreference = preferences.MealTypePreferences
                .FirstOrDefault(mp => mp.MealType == recipe.MealType);
            
            if (mealTypePreference != null)
            {
                score += mealTypePreference.PreferenceRatio * 0.20;
            }
        }

        // Variety score - avoid recently used recipes (15% weight)
        if (preferences.RecentlyUsedRecipeIds.Contains(recipe.Id))
        {
            score += 0.0; // No bonus for recently used
        }
        else
        {
            score += 0.15; // Bonus for variety
        }

        // Favorite recipe bonus (10% weight)
        if (preferences.FavoriteRecipeIds.Contains(recipe.Id))
        {
            score += 0.10;
        }

        return Math.Max(0.0, Math.Min(5.0, score)); // Ensure score is between 0-5
    }

    /// <summary>
    /// Get all allergens for a recipe (both direct recipe allergens and ingredient allergens)
    /// </summary>
    private List<int> GetAllRecipeAllergens(Recipe recipe)
    {
        var allergenIds = new List<int>();

        // Add direct recipe allergens
        if (recipe.RecipeAllergens != null)
        {
            allergenIds.AddRange(recipe.RecipeAllergens
                .Where(ra => ra.AllergenId.HasValue)
                .Select(ra => ra.AllergenId.Value));
        }

        // Add ingredient allergens
        if (recipe.RecipeIngredients != null)
        {
            var ingredientAllergenIds = recipe.RecipeIngredients
                .Where(ri => ri.Ingredient != null && ri.Ingredient.IngredientAllergens != null)
                .SelectMany(ri => ri.Ingredient.IngredientAllergens)
                .Where(ia => ia.AllergenId.HasValue)
                .Select(ia => ia.AllergenId.Value);

            allergenIds.AddRange(ingredientAllergenIds);
        }

        return allergenIds.Distinct().ToList();
    }
}

// Supporting classes for user preferences
public class UserCuisinePreferences
{
    public List<CuisinePreference> CuisinePreferences { get; set; } = new();
    public List<MealTypePreference> MealTypePreferences { get; set; } = new();
    public List<int> RecentlyUsedRecipeIds { get; set; } = new();
    public List<int> FavoriteRecipeIds { get; set; } = new();
}

public class CuisinePreference
{
    public string CuisineType { get; set; } = string.Empty;
    public int UsageCount { get; set; }
    public double PreferenceRatio { get; set; }
    public double CompletionRate { get; set; }
}

public class MealTypePreference
{
    public string MealType { get; set; } = string.Empty;
    public int UsageCount { get; set; }
    public double PreferenceRatio { get; set; }
} 