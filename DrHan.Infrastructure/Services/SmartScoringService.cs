using DrHan.Application.DTOs.MealPlans;
using DrHan.Application.Interfaces.Repository;
using DrHan.Application.Services.SmartScoringService;
using DrHan.Domain.Constants;
using DrHan.Domain.Entities.MealPlans;
using DrHan.Domain.Entities.Recipes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DrHan.Infrastructure.Services;

public class SmartScoringService : ISmartScoringService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SmartScoringService> _logger;

    // Scoring weights (totaling 1.0)
    private const double QUALITY_WEIGHT = 0.30;      // Recipe rating & popularity
    private const double VARIETY_WEIGHT = 0.25;      // Avoid recent repetition
    private const double TIME_WEIGHT = 0.20;         // Cooking time appropriateness
    private const double NUTRITION_WEIGHT = 0.15;    // Nutritional target matching
    private const double USER_PREFERENCE_WEIGHT = 0.10; // User behavior learning

    public SmartScoringService(
        IUnitOfWork unitOfWork,
        ILogger<SmartScoringService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Recipe> SelectSmartRecipeAsync(List<Recipe> filteredRecipes, SmartSelectionContext context)
    {
        if (!filteredRecipes.Any())
        {
            _logger.LogWarning("No filtered recipes provided for smart selection");
            return null;
        }

        if (filteredRecipes.Count == 1)
        {
            return filteredRecipes.First(); // No need to score single recipe
        }

        try
        {
            // Calculate scores for all recipes
            var scoredRecipes = new List<(Recipe Recipe, RecipeScore Score)>();

            foreach (var recipe in filteredRecipes)
            {
                var score = await CalculateRecipeScoreAsync(recipe, context);
                scoredRecipes.Add((recipe, score));
            }

            // Select highest scoring recipe
            var bestRecipe = scoredRecipes
                .OrderByDescending(sr => sr.Score.TotalScore)
                .First();

            _logger.LogInformation("Smart selection: Recipe {RecipeId} '{RecipeName}' scored {Score:F2} for user {UserId}",
                bestRecipe.Recipe.Id, bestRecipe.Recipe.Name, bestRecipe.Score.TotalScore, context.UserId);

            return bestRecipe.Recipe;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in smart recipe selection, falling back to random");
            
            // Fallback to random selection if scoring fails
            var random = new Random();
            return filteredRecipes[random.Next(filteredRecipes.Count)];
        }
    }

    public async Task<RecipeScore> CalculateRecipeScoreAsync(Recipe recipe, SmartSelectionContext context)
    {
        var score = new RecipeScore
        {
            RecipeId = recipe.Id
        };

        try
        {
            // 1. Calculate Quality Score (30%)
            score.QualityScore = CalculateQualityScore(recipe);

            // 2. Calculate Variety Score (25%) - Database driven
            score.VarietyScore = await CalculateVarietyScoreAsync(recipe.Id, context);

            // 3. Calculate Time Score (20%) - Algorithm driven
            score.TimeScore = CalculateTimeScore(recipe, context);

            // 4. Calculate Nutritional Score (15%) - Database driven
            score.NutritionalScore = CalculateNutritionalScore(recipe, context);

            // 5. Calculate User Preference Score (10%) - Database driven
            score.UserPreferenceScore = await CalculateUserPreferenceScoreAsync(recipe, context);

            // Calculate weighted total score
            score.TotalScore = 
                (score.QualityScore * QUALITY_WEIGHT) +
                (score.VarietyScore * VARIETY_WEIGHT) +
                (score.TimeScore * TIME_WEIGHT) +
                (score.NutritionalScore * NUTRITION_WEIGHT) +
                (score.UserPreferenceScore * USER_PREFERENCE_WEIGHT);

            // Generate breakdown for debugging
            score.ScoreBreakdown = $"Quality:{score.QualityScore:F2}({QUALITY_WEIGHT}), " +
                                 $"Variety:{score.VarietyScore:F2}({VARIETY_WEIGHT}), " +
                                 $"Time:{score.TimeScore:F2}({TIME_WEIGHT}), " +
                                 $"Nutrition:{score.NutritionalScore:F2}({NUTRITION_WEIGHT}), " +
                                 $"Preference:{score.UserPreferenceScore:F2}({USER_PREFERENCE_WEIGHT})";

            return score;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating score for recipe {RecipeId}", recipe.Id);
            
            // Return neutral score on error
            return new RecipeScore
            {
                RecipeId = recipe.Id,
                TotalScore = 2.5, // Neutral score
                QualityScore = 2.5,
                VarietyScore = 0.5,
                TimeScore = 0.5,
                NutritionalScore = 0.5,
                UserPreferenceScore = 0.5,
                ScoreBreakdown = "Error in calculation"
            };
        }
    }

    private double CalculateQualityScore(Recipe recipe)
    {
        try
        {
            var score = 0.0;

            // Primary: Recipe rating (0-5 scale)
            var rating = (double)(recipe.RatingAverage ?? 0);
            score += rating * 0.7; // 70% weight for rating

            // Secondary: Popularity (likes count normalized)
            var likes = recipe.LikesCount ?? 0;
            var likesScore = Math.Min(likes / 100.0, 1.0); // Cap at 100 likes = 1.0 score
            score += likesScore * 0.2; // 20% weight for popularity

            // Tertiary: Professional vs custom (slight bias toward tested recipes)
            var isCustom = recipe.IsCustom ?? false;
            var professionalBonus = isCustom ? 0.0 : 0.1; // 10% bonus for non-custom
            score += professionalBonus;

            return Math.Min(score, 5.0); // Cap at 5.0
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error calculating quality score for recipe {RecipeId}, using neutral score", recipe.Id);
            return 2.5; // Neutral score on error
        }
    }

    private async Task<double> CalculateVarietyScoreAsync(int recipeId, SmartSelectionContext context)
    {
        try
        {
            // Get recent meal entries for this user - simplified query
            var cutoffDate = context.Date.AddDays(-14); // Look back 2 weeks
            
            var allUserEntries = await _unitOfWork.Repository<MealPlanEntry>()
                .ListAsync(
                    filter: mpe => mpe.MealDate >= cutoffDate,
                    includeProperties: query => query.Include(mpe => mpe.MealPlan)
                );

            // Filter in memory for user and recipe
            var recentEntries = allUserEntries
                .Where(mpe => mpe.MealPlan != null && 
                             mpe.MealPlan.UserId == context.UserId &&
                             mpe.RecipeId == recipeId)
                .OrderByDescending(mpe => mpe.MealDate)
                .ToList();

            if (!recentEntries.Any())
            {
                return 1.0; // Never used recently = full variety score
            }

            // Find most recent usage
            var mostRecentEntry = recentEntries.First();
            var daysSinceUsed = (context.Date.ToDateTime(TimeOnly.MinValue) - mostRecentEntry.MealDate.ToDateTime(TimeOnly.MinValue)).Days;

            // Scoring based on recency
            return daysSinceUsed switch
            {
                < 1 => 0.0,   // Used today = no variety
                < 3 => 0.1,   // Used 1-2 days ago = very low variety
                < 7 => 0.3,   // Used this week = low variety
                < 14 => 0.7,  // Used last 2 weeks = medium variety
                _ => 1.0      // Used long ago = full variety
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error calculating variety score for recipe {RecipeId}", recipeId);
            return 0.5; // Neutral score on error
        }
    }

    private double CalculateTimeScore(Recipe recipe, SmartSelectionContext context)
    {
        var prepTime = recipe.PrepTimeMinutes ?? 0;
        var cookTime = recipe.CookTimeMinutes ?? 0;
        var totalTime = prepTime + cookTime;

        // Get time expectations based on meal type and context
        var timeScore = context.MealType switch
        {
            MealTypeConstants.BREAKFAST => CalculateBreakfastTimeScore(totalTime, context),
            MealTypeConstants.LUNCH => CalculateLunchTimeScore(totalTime, context),
            MealTypeConstants.DINNER => CalculateDinnerTimeScore(totalTime, context),
            MealTypeConstants.SNACK => CalculateSnackTimeScore(totalTime, context),
            _ => CalculateDefaultTimeScore(totalTime)
        };

        return Math.Max(0.0, Math.Min(1.0, timeScore)); // Ensure 0-1 range
    }

    private double CalculateBreakfastTimeScore(int totalTime, SmartSelectionContext context)
    {
        if (context.IsRushHour || (context.CurrentTime.Hour >= 7 && context.CurrentTime.Hour <= 8))
        {
            // Weekday morning rush - prefer very quick meals
            return totalTime switch
            {
                <= 10 => 1.0,   // Perfect for rush
                <= 20 => 0.7,   // Acceptable
                <= 30 => 0.4,   // Getting slow
                <= 45 => 0.2,   // Too slow for rush
                _ => 0.1        // Way too slow
            };
        }

        if (context.IsWeekend || context.CurrentTime.Hour >= 9)
        {
            // Weekend or leisurely breakfast - more time available
            return totalTime switch
            {
                <= 45 => 1.0,   // Perfect for weekend
                <= 60 => 0.8,   // Still good
                <= 90 => 0.5,   // Getting elaborate
                _ => 0.3        // Very elaborate
            };
        }

        return 0.5; // Neutral for other times
    }

    private double CalculateLunchTimeScore(int totalTime, SmartSelectionContext context)
    {
        if (context.IsRushHour)
        {
            // Quick lunch needed
            return totalTime switch
            {
                <= 20 => 1.0,   // Perfect for quick lunch
                <= 35 => 0.8,   // Good
                <= 50 => 0.5,   // Pushing it
                _ => 0.2        // Too slow for rush
            };
        }

        // Normal lunch timing
        return totalTime switch
        {
            <= 45 => 1.0,   // Good lunch timing
            <= 60 => 0.8,   // Acceptable
            <= 90 => 0.5,   // Getting long
            _ => 0.3        // Too elaborate for lunch
        };
    }

    private double CalculateDinnerTimeScore(int totalTime, SmartSelectionContext context)
    {
        if (context.IsWeekend)
        {
            // Weekend dinner - more time for elaborate meals
            return totalTime switch
            {
                <= 120 => 1.0,  // Perfect for weekend dinner
                <= 180 => 0.8,  // Elaborate but acceptable
                _ => 0.5        // Very elaborate
            };
        }

        // Weekday dinner
        return totalTime switch
        {
            <= 60 => 1.0,   // Perfect for weekday
            <= 90 => 0.8,   // Good
            <= 120 => 0.6,  // Getting long
            _ => 0.4        // Too elaborate for weekday
        };
    }

    private double CalculateSnackTimeScore(int totalTime, SmartSelectionContext context)
    {
        // Snacks should generally be quick
        return totalTime switch
        {
            <= 5 => 1.0,    // Perfect snack
            <= 15 => 0.8,   // Quick snack
            <= 30 => 0.4,   // Getting elaborate for snack
            _ => 0.1        // Too complex for snack
        };
    }

    private double CalculateDefaultTimeScore(int totalTime)
    {
        // Default scoring for unknown meal types
        return totalTime switch
        {
            <= 30 => 1.0,
            <= 60 => 0.8,
            <= 90 => 0.6,
            _ => 0.4
        };
    }

    private double CalculateNutritionalScore(Recipe recipe, SmartSelectionContext context)
    {
        try
        {
            var nutrition = recipe.RecipeNutritions?.FirstOrDefault();
            if (nutrition == null)
            {
                return 0.5; 
            }

           

            return 0.5;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error calculating nutritional score for recipe {RecipeId}", recipe.Id);
            return 0.5; // Neutral score on error
        }
    }

    private async Task<double> CalculateUserPreferenceScoreAsync(Recipe recipe, SmartSelectionContext context)
    {
        try
        {
            var score = 0.0;

            // Cuisine preference (60% of preference score)
            if (!string.IsNullOrEmpty(recipe.CuisineType))
            {
                var cuisinePreferences = await GetUserCuisinePreferencesAsync(context.UserId);
                var cuisinePreference = cuisinePreferences.FirstOrDefault(cp => cp.CuisineType == recipe.CuisineType);
                
                if (cuisinePreference != null)
                {
                    score += cuisinePreference.PreferenceRatio * 0.6;
                }
                else
                {
                    score += 0.3; // Neutral for unknown cuisines
                }
            }

            // Recipe completion rate (40% of preference score)
            var completionRates = await GetUserRecipeCompletionRatesAsync(context.UserId);
            if (completionRates.ContainsKey(recipe.Id))
            {
                score += completionRates[recipe.Id] * 0.4;
            }
            else
            {
                score += 0.2; // Slight penalty for untried recipes
            }

            return Math.Max(0.0, Math.Min(1.0, score));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error calculating user preference score for recipe {RecipeId}", recipe.Id);
            return 0.5; // Neutral score on error
        }
    }

    public async Task<List<UserCuisinePreference>> GetUserCuisinePreferencesAsync(int userId)
    {
        try
        {
            // Get all meal entries with meal plan and recipe included
            var allMealEntries = await _unitOfWork.Repository<MealPlanEntry>()
                .ListAsync(
                    filter: mpe => mpe.RecipeId.HasValue,
                    includeProperties: query => query
                        .Include(mpe => mpe.MealPlan)
                        .Include(mpe => mpe.Recipe)
                );

            // Filter by user in memory
            var userMealEntries = allMealEntries
                .Where(mpe => mpe.MealPlan != null && mpe.MealPlan.UserId == userId)
                .ToList();

            if (!userMealEntries.Any())
            {
                return new List<UserCuisinePreference>();
            }

            // Group by cuisine type and calculate statistics
            var cuisineStats = userMealEntries
                .Where(mpe => mpe.Recipe != null && !string.IsNullOrEmpty(mpe.Recipe.CuisineType))
                .GroupBy(mpe => mpe.Recipe.CuisineType)
                .Select(group => new UserCuisinePreference
                {
                    CuisineType = group.Key,
                    UsageCount = group.Count(),
                    PreferenceRatio = (double)group.Count() / userMealEntries.Count,
                    CompletionRate = group.Count(mpe => mpe.IsCompleted) / (double)group.Count()
                })
                .OrderByDescending(cp => cp.UsageCount)
                .ToList();

            return cuisineStats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user cuisine preferences for user {UserId}", userId);
            return new List<UserCuisinePreference>();
        }
    }

    public async Task<List<int>> GetRecentlyUsedRecipesAsync(int userId, int daysBack = 14)
    {
        try
        {
            var cutoffDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-daysBack));
            
            // Get all recent entries with meal plan included
            var recentEntries = await _unitOfWork.Repository<MealPlanEntry>()
                .ListAsync(
                    filter: mpe => mpe.MealDate >= cutoffDate && mpe.RecipeId.HasValue,
                    includeProperties: query => query.Include(mpe => mpe.MealPlan)
                );

            // Filter by user in memory and extract recipe IDs
            var recentRecipeIds = recentEntries
                .Where(mpe => mpe.MealPlan != null && mpe.MealPlan.UserId == userId)
                .Select(mpe => mpe.RecipeId.Value)
                .Distinct()
                .ToList();

            return recentRecipeIds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recently used recipes for user {UserId}", userId);
            return new List<int>();
        }
    }

    public async Task<Dictionary<int, double>> GetUserRecipeCompletionRatesAsync(int userId)
    {
        try
        {
            // Get all meal entries with meal plan included
            var allMealEntries = await _unitOfWork.Repository<MealPlanEntry>()
                .ListAsync(
                    filter: mpe => mpe.RecipeId.HasValue,
                    includeProperties: query => query.Include(mpe => mpe.MealPlan)
                );

            // Filter by user in memory
            var userMealEntries = allMealEntries
                .Where(mpe => mpe.MealPlan != null && mpe.MealPlan.UserId == userId)
                .ToList();

            var completionRates = userMealEntries
                .GroupBy(mpe => mpe.RecipeId.Value)
                .Where(group => group.Count() >= 2) // Only include recipes tried at least twice
                .ToDictionary(
                    group => group.Key,
                    group => group.Count(mpe => mpe.IsCompleted) / (double)group.Count()
                );

            return completionRates;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user recipe completion rates for user {UserId}", userId);
            return new Dictionary<int, double>();
        }
    }
} 