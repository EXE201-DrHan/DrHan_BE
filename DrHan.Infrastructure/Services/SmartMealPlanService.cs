using AutoMapper;
using DrHan.Application.Commons;
using DrHan.Application.DTOs.MealPlans;
using DrHan.Application.DTOs.Recipes;
using DrHan.Application.Interfaces.Repository;
using DrHan.Application.Interfaces.Services;
using DrHan.Application.Interfaces.Services.CacheService;
using DrHan.Application.StaticQuery;
using DrHan.Domain.Entities.MealPlans;
using DrHan.Domain.Entities.Recipes;
using DrHan.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DrHan.Infrastructure.Services;

public class SmartMealPlanService : ISmartMealPlanService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<SmartMealPlanService> _logger;
    private readonly ICacheService _cacheService;
    private readonly ICacheKeyService _cacheKeyService;

    // Meal type distribution weights for smart planning
    private readonly Dictionary<string, double> _mealTypeWeights = new()
    {
        { "Breakfast", 0.20 }, // Light, quick meals
        { "Lunch", 0.30 },     // Moderate, often portable
        { "Dinner", 0.40 },    // Main meal, more complex
        { "Snack", 0.10 }      // Light, healthy options
    };

    // Cache expiration settings
    private static readonly TimeSpan UserAllergiesCacheExpiration = TimeSpan.FromHours(2);
    private static readonly TimeSpan RecipeFilterCacheExpiration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan TemplatesCacheExpiration = TimeSpan.FromHours(6);

    public SmartMealPlanService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<SmartMealPlanService> logger,
        ICacheService cacheService,
        ICacheKeyService cacheKeyService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _cacheService = cacheService;
        _cacheKeyService = cacheKeyService;
    }

    public async Task<AppResponse<MealPlanDto>> GenerateSmartMealPlanAsync(GenerateMealPlanDto request, int userId)
    {
        var response = new AppResponse<MealPlanDto>();

        try
        {
            // Validate meal plan type and FamilyId relationship
            if (request.PlanType?.ToLower() == "personal")
            {
                if (request.FamilyId.HasValue)
                {
                    return response.SetErrorResponse("PlanType", "Personal meal plans cannot be associated with a family");
                }
            }
            else if (request.PlanType?.ToLower() == "family")
            {
                if (!request.FamilyId.HasValue)
                {
                    return response.SetErrorResponse("PlanType", "Family meal plans must be associated with a family");
                }
            }

            // Validate FamilyId if provided
            if (request.FamilyId.HasValue)
            {
                var familyExists = await _unitOfWork.Repository<DrHan.Domain.Entities.Families.Family>()
                    .ExistsAsync(f => f.Id == request.FamilyId.Value);
                
                if (!familyExists)
                {
                    return response.SetErrorResponse("Family", "The specified family does not exist");
                }
            }

            // 1. Create the meal plan
            var mealPlan = new MealPlan
            {
                UserId = userId,
                FamilyId = request.FamilyId,
                Name = request.Name,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                PlanType = request.PlanType,
                Notes = "Auto-generated smart meal plan"
            };

            await _unitOfWork.Repository<MealPlan>().AddAsync(mealPlan);
            await _unitOfWork.CompleteAsync();

            // 2. Get user allergies and preferences
            var userAllergies = await GetUserAllergiesAsync(userId);
            
            // 3. Generate meal entries for each day
            var totalDays = (request.EndDate.ToDateTime(TimeOnly.MinValue) - request.StartDate.ToDateTime(TimeOnly.MinValue)).Days + 1;
            var mealEntries = new List<MealPlanEntry>();

            for (int dayOffset = 0; dayOffset < totalDays; dayOffset++)
            {
                var currentDate = request.StartDate.AddDays(dayOffset);
                var dailyMeals = await GenerateDailyMealsAsync(mealPlan.Id, currentDate, request.Preferences, userAllergies);
                mealEntries.AddRange(dailyMeals);
            }

            // 4. Save all meal entries
            foreach (var entry in mealEntries)
            {
                await _unitOfWork.Repository<MealPlanEntry>().AddAsync(entry);
            }
            await _unitOfWork.CompleteAsync();

            // 5. Load the complete meal plan with entries
            var completeMealPlan = await _unitOfWork.Repository<MealPlan>()
                .ListAsync(
                    filter: mp => mp.Id == mealPlan.Id,
                    includeProperties: query => query.Include(mp => mp.MealPlanEntries).ThenInclude(mpe => mpe.Recipe)
                );

            var mealPlanDto = _mapper.Map<MealPlanDto>(completeMealPlan.FirstOrDefault());

            _logger.LogInformation("Smart meal plan generated with {MealCount} meals for user {UserId}", 
                mealEntries.Count, userId);

            return response.SetSuccessResponse(mealPlanDto, 
                "Success", $"Smart meal plan generated successfully with {mealEntries.Count} meals");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating smart meal plan for user {UserId}", userId);
            return response.SetErrorResponse("Error", "Failed to generate smart meal plan");
        }
    }

    public async Task<AppResponse<List<int>>> GetRecommendedRecipesAsync(MealPlanPreferencesDto preferences, int userId, string mealType)
    {
        var response = new AppResponse<List<int>>();

        try
        {
            var userAllergies = await GetUserAllergiesAsync(userId);
            var recipes = await FilterRecipesByPreferencesAsync(preferences, userAllergies, mealType);
            
            var recipeIds = recipes.Select(r => r.Id).ToList();
            return response.SetSuccessResponse(recipeIds, "Success", $"Found {recipeIds.Count} recommended recipes");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recommended recipes for user {UserId}", userId);
            return response.SetErrorResponse("Error", "Failed to get recipe recommendations");
        }
    }

    public async Task<AppResponse<MealPlanDto>> ApplyTemplateAsync(int templateId, CreateMealPlanDto mealPlan, int userId)
    {
        // Template implementation would go here
        var response = new AppResponse<MealPlanDto>();
        return response.SetErrorResponse("NotImplemented", "Template functionality not yet implemented");
    }

    public async Task<AppResponse<bool>> BulkFillMealsAsync(BulkFillMealsDto request, int userId)
    {
        var response = new AppResponse<bool>();

        try
        {
            // Verify meal plan ownership
            var mealPlan = await _unitOfWork.Repository<MealPlan>()
                .GetEntityByIdAsync(request.MealPlanId);

            if (mealPlan == null || mealPlan.UserId != userId)
            {
                return response.SetErrorResponse("Authorization", "Meal plan not found or access denied");
            }

            var mealEntries = new List<MealPlanEntry>();
            var targetDates = request.TargetDates.Any() ? request.TargetDates : GetDateRange(mealPlan.StartDate, mealPlan.EndDate);

            foreach (var date in targetDates)
            {
                var recipeId = SelectRecipeByPattern(request.RecipeIds, request.FillPattern, date);
                
                mealEntries.Add(new MealPlanEntry
                {
                    MealPlanId = request.MealPlanId,
                    MealDate = date,
                    MealType = request.MealType,
                    RecipeId = recipeId,
                    Servings = 1
                });
            }

            foreach (var entry in mealEntries)
            {
                await _unitOfWork.Repository<MealPlanEntry>().AddAsync(entry);
            }
            await _unitOfWork.CompleteAsync();

            // Invalidate meal plan cache after bulk operations
            await InvalidateMealPlanCacheAsync(request.MealPlanId, userId);

            _logger.LogInformation("Bulk filled {Count} meals for meal plan {MealPlanId}", 
                mealEntries.Count, request.MealPlanId);

            return response.SetSuccessResponse(true, "Success", $"Successfully filled {mealEntries.Count} meal slots");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk filling meals for user {UserId}", userId);
            return response.SetErrorResponse("Error", "Failed to bulk fill meals");
        }
    }

    public async Task<AppResponse<List<MealPlanTemplateDto>>> GetAvailableTemplatesAsync()
    {
        var cacheKey = _cacheKeyService.Custom("mealplan", "templates");
        
        var templates = await _cacheService.GetAsync<List<MealPlanTemplateDto>>(cacheKey, async () =>
        {
            // Return predefined templates - could be stored in database later
            return new List<MealPlanTemplateDto>
            {
                new MealPlanTemplateDto
                {
                    Id = 1,
                    Name = "Busy Professional Week",
                    Description = "Quick 15-minute meals for busy weekdays",
                    Category = "Quick & Easy",
                    Duration = 7
                },
                new MealPlanTemplateDto
                {
                    Id = 2,
                    Name = "Mediterranean Week",
                    Description = "Healthy Mediterranean-style meals",
                    Category = "Healthy",
                    Duration = 7
                },
                new MealPlanTemplateDto
                {
                    Id = 3,
                    Name = "Family Friendly Month",
                    Description = "Kid-approved meals for the whole family",
                    Category = "Family",
                    Duration = 30
                }
            };
        }, TemplatesCacheExpiration);
        
        var response = new AppResponse<List<MealPlanTemplateDto>>();
        return response.SetSuccessResponse(templates, "Success", "Templates retrieved successfully");
    }

    // PRIVATE METHODS WITH CACHING

    private async Task<List<int>> GetUserAllergiesAsync(int userId)
    {
        var cacheKey = _cacheKeyService.Custom("user", userId, "allergies");
        
        return await _cacheService.GetAsync<List<int>>(cacheKey, async () =>
        {
            try
            {
                var userAllergies = await _unitOfWork.Repository<UserAllergy>()
                    .ListAsync(filter: ua => ua.UserId == userId);
                return userAllergies
                    .Where(ua => ua.AllergenId.HasValue)
                    .Select(ua => ua.AllergenId.Value)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving allergies for user {UserId}", userId);
                return new List<int>();
            }
        }, UserAllergiesCacheExpiration);
    }

    private async Task<List<MealPlanEntry>> GenerateDailyMealsAsync(int mealPlanId, DateOnly date, MealPlanPreferencesDto preferences, List<int> userAllergies)
    {
        var mealTypes = preferences.PreferredMealTypes.Any() 
            ? preferences.PreferredMealTypes 
            : new List<string> { "Breakfast", "Lunch", "Dinner" };

        var dailyMeals = new List<MealPlanEntry>();

        foreach (var mealType in mealTypes)
        {
            var recipes = await FilterRecipesByPreferencesAsync(preferences, userAllergies, mealType);
            if (recipes.Any())
            {
                var selectedRecipe = SelectRandomRecipe(recipes);
                dailyMeals.Add(new MealPlanEntry
                {
                    MealPlanId = mealPlanId,
                    MealDate = date,
                    MealType = mealType,
                    RecipeId = selectedRecipe.Id,
                    Servings = CalculateServings(mealType),
                    Notes = "Auto-generated"
                });
            }
        }

        return dailyMeals;
    }

    private async Task<List<Recipe>> FilterRecipesByPreferencesAsync(MealPlanPreferencesDto preferences, List<int> userAllergies, string mealType)
    {
        // Create cache key based on preferences and allergies
        var preferencesHash = GeneratePreferencesHash(preferences, userAllergies, mealType);
        var cacheKey = _cacheKeyService.Custom("recipes", "filtered", mealType, preferencesHash);

        return await _cacheService.GetAsync(cacheKey, async () =>
        {
            try
            {
                // Build filter using static query
                var filter = MealPlanRecipeQuery.BuildMealPlanFilter(preferences, userAllergies, mealType);
                var orderBy = MealPlanRecipeQuery.BuildMealPlanOrderBy(mealType);
                var includes = MealPlanRecipeQuery.BuildMealPlanIncludes();
                var recipeCount = MealPlanRecipeQuery.GetRecommendedRecipeCount(mealType, preferences);

                // Execute query using repository with optimizations
                var recipes = await _unitOfWork.Repository<Recipe>()
                    .ListAsync(
                        filter: filter,
                        orderBy: orderBy,
                        includeProperties: includes
                    );

                // Take recommended count for performance
                return recipes.Take(recipeCount).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error filtering recipes for meal type {MealType}", mealType);
                
                // Fallback to simple query if complex filtering fails
                return await GetFallbackRecipes(mealType);
            }
        }, RecipeFilterCacheExpiration);
    }

    private async Task<List<Recipe>> GetFallbackRecipes(string mealType)
    {
        var cacheKey = _cacheKeyService.Custom("recipes", "fallback", mealType);
        
        return await _cacheService.GetAsync(cacheKey, async () =>
        {
            try
            {
                var recipes = await _unitOfWork.Repository<Recipe>()
                    .ListAsync(
                        orderBy: q => q.OrderByDescending(r => r.RatingAverage)
                    );

                return recipes.Take(20).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in fallback recipe query for meal type {MealType}", mealType);
                return new List<Recipe>();
            }
        }, RecipeFilterCacheExpiration);
    }

    private Recipe SelectRandomRecipe(List<Recipe> recipes)
    {
        var random = new Random();
        return recipes[random.Next(recipes.Count)];
    }

    private decimal CalculateServings(string mealType)
    {
        return mealType.ToLower() switch
        {
            "breakfast" => 1,
            "lunch" => 1,
            "dinner" => 2, // Assuming dinner might have leftovers
            "snack" => 1,
            _ => 1
        };
    }

    private List<DateOnly> GetDateRange(DateOnly startDate, DateOnly endDate)
    {
        var dates = new List<DateOnly>();
        var current = startDate;
        
        while (current <= endDate)
        {
            dates.Add(current);
            current = current.AddDays(1);
        }
        
        return dates;
    }

    private int SelectRecipeByPattern(List<int> recipeIds, string pattern, DateOnly date)
    {
        return pattern.ToLower() switch
        {
            "rotate" => recipeIds[date.DayNumber % recipeIds.Count],
            "random" => recipeIds[new Random().Next(recipeIds.Count)],
            "same" => recipeIds.First(),
            _ => recipeIds.First()
        };
    }

    private string GeneratePreferencesHash(MealPlanPreferencesDto preferences, List<int> userAllergies, string mealType)
    {
        // Create a simple hash based on key preference values
        var cuisineTypes = preferences?.CuisineTypes != null ? string.Join(",", preferences.CuisineTypes.OrderBy(x => x)) : "";
        var dietaryGoals = preferences?.DietaryGoals != null ? string.Join(",", preferences.DietaryGoals.OrderBy(x => x)) : "";
        var hashSource = $"{mealType}_{cuisineTypes}_{dietaryGoals}_{preferences?.MaxCookingTime}_{preferences?.BudgetRange}_{string.Join(",", userAllergies.OrderBy(x => x))}";
        return hashSource.GetHashCode().ToString();
    }

    private async Task InvalidateMealPlanCacheAsync(int mealPlanId, int userId)
    {
        try
        {
            // Invalidate specific meal plan cache
            var mealPlanCacheKey = _cacheKeyService.Entity<MealPlan>(mealPlanId);
            await _cacheService.RemoveAsync(mealPlanCacheKey);

            // Invalidate user's meal plan list cache pattern
            var userMealPlansPattern = _cacheKeyService.Custom("user", userId, "mealplans", "*");
            await _cacheService.RemoveByPatternAsync(userMealPlansPattern);

            _logger.LogInformation("Invalidated meal plan cache for meal plan {MealPlanId} and user {UserId}", mealPlanId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate meal plan cache for meal plan {MealPlanId}", mealPlanId);
        }
    }
}