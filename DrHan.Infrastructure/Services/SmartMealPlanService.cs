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
            
            // 3. Generate meal entries for each day (OPTIMIZED)
            var totalDays = (request.EndDate.ToDateTime(TimeOnly.MinValue) - request.StartDate.ToDateTime(TimeOnly.MinValue)).Days + 1;
            var targetDates = GetDateRange(request.StartDate, request.EndDate);
            
            // Determine meal types to generate
            var mealTypes = request.Preferences?.PreferredMealTypes?.Any() == true 
                ? request.Preferences.PreferredMealTypes 
                : new List<string> { "Breakfast", "Lunch", "Dinner" };

            // PRE-FETCH ALL RECIPES FOR ALL MEAL TYPES (PERFORMANCE OPTIMIZATION)
            var recipesByMealType = new Dictionary<string, List<Recipe>>();
            foreach (var mealType in mealTypes)
            {
                var recipes = await FilterRecipesByPreferencesAsync(request.Preferences, userAllergies, mealType);
                recipesByMealType[mealType] = recipes;
            }

            var mealEntries = new List<MealPlanEntry>();

            // BATCH GENERATE ALL MEALS (PERFORMANCE OPTIMIZATION)
            foreach (var date in targetDates)
            {
                foreach (var mealType in mealTypes)
                {
                    if (recipesByMealType.ContainsKey(mealType) && recipesByMealType[mealType].Any())
                    {
                        var selectedRecipe = SelectRandomRecipe(recipesByMealType[mealType]);
                        mealEntries.Add(new MealPlanEntry
                        {
                            MealPlanId = mealPlan.Id,
                            MealDate = date,
                            MealType = mealType,
                            RecipeId = selectedRecipe.Id,
                            Servings = CalculateServings(mealType),
                            Notes = "Auto-generated"
                        });
                    }
                }
            }

            // 4. BATCH SAVE ALL MEAL ENTRIES (PERFORMANCE OPTIMIZATION)
            if (mealEntries.Any())
            {
                await _unitOfWork.Repository<MealPlanEntry>().AddRangeAsync(mealEntries);
            }
            await _unitOfWork.CompleteAsync();

            // 5. BUILD RESULT FROM MEMORY (AVOID EXTRA QUERY)
            mealPlan.MealPlanEntries = mealEntries;
            var mealPlanDto = _mapper.Map<MealPlanDto>(mealPlan);

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

    public async Task<AppResponse<MealPlanDto>> GenerateSmartMealsAsync(int mealPlanId, GenerateSmartMealsDto request, int userId)
    {
        var response = new AppResponse<MealPlanDto>();

        try
        {
            // Get the existing meal plan
            var mealPlan = await _unitOfWork.Repository<MealPlan>()
                .ListAsync(
                    filter: mp => mp.Id == mealPlanId && mp.UserId == userId,
                    includeProperties: query => query.Include(mp => mp.MealPlanEntries)
                );

            var existingMealPlan = mealPlan.FirstOrDefault();
            if (existingMealPlan == null)
            {
                return response.SetErrorResponse("NotFound", "Meal plan not found or access denied");
            }

            // Get user allergies and preferences
            var userAllergies = await GetUserAllergiesAsync(userId);
            
            // Determine target dates
            var targetDates = request.TargetDates.Any() 
                ? request.TargetDates 
                : GetDateRange(existingMealPlan.StartDate, existingMealPlan.EndDate);

            // Determine meal types to generate
            var mealTypes = request.MealTypes.Any() 
                ? request.MealTypes 
                : (request.Preferences?.PreferredMealTypes?.Any() == true 
                    ? request.Preferences.PreferredMealTypes 
                    : new List<string> { "Breakfast", "Lunch", "Dinner" });

            // PRE-FETCH ALL RECIPES FOR ALL MEAL TYPES (PERFORMANCE OPTIMIZATION)
            var recipesByMealType = new Dictionary<string, List<Recipe>>();
            foreach (var mealType in mealTypes)
            {
                var recipes = await FilterRecipesByPreferencesAsync(request.Preferences, userAllergies, mealType);
                recipesByMealType[mealType] = recipes;
            }

            var generatedMeals = new List<MealPlanEntry>();
            var mealsToDelete = new List<MealPlanEntry>();

            // PROCESS ALL MEALS IN MEMORY FIRST (BATCH PROCESSING)
            foreach (var date in targetDates)
            {
                foreach (var mealType in mealTypes)
                {
                    // Check if meal already exists for this date and type
                    var existingMeal = existingMealPlan.MealPlanEntries
                        .FirstOrDefault(me => me.MealDate == date && me.MealType.Equals(mealType, StringComparison.OrdinalIgnoreCase));

                    // Skip if meal exists and we're not replacing, or if it's marked as favorite and we preserve favorites
                    if (existingMeal != null)
                    {
                        if (!request.ReplaceExisting)
                        {
                            _logger.LogDebug("Skipping existing meal for {Date} {MealType}", date, mealType);
                            continue;
                        }

                        if (request.PreserveFavorites && IsMealFavorite(existingMeal))
                        {
                            _logger.LogDebug("Preserving favorite meal for {Date} {MealType}", date, mealType);
                            continue;
                        }

                        // Mark for deletion
                        mealsToDelete.Add(existingMeal);
                    }

                    // Generate new meal using pre-fetched recipes
                    if (recipesByMealType.ContainsKey(mealType) && recipesByMealType[mealType].Any())
                    {
                        var selectedRecipe = SelectRandomRecipe(recipesByMealType[mealType]);
                        var newMealEntry = new MealPlanEntry
                        {
                            MealPlanId = mealPlanId,
                            MealDate = date,
                            MealType = mealType,
                            RecipeId = selectedRecipe.Id,
                            Servings = CalculateServings(mealType),
                            Notes = "Smart-generated"
                        };

                        generatedMeals.Add(newMealEntry);
                    }
                }
            }

            // BATCH DATABASE OPERATIONS (PERFORMANCE OPTIMIZATION)
            if (mealsToDelete.Any())
            {
                _unitOfWork.Repository<MealPlanEntry>().DeleteRange(mealsToDelete);
            }

            if (generatedMeals.Any())
            {
                await _unitOfWork.Repository<MealPlanEntry>().AddRangeAsync(generatedMeals);
            }

            await _unitOfWork.CompleteAsync();

            // Load the updated meal plan with entries (AVOID EXTRA QUERY)
            existingMealPlan.MealPlanEntries = existingMealPlan.MealPlanEntries
                .Where(me => !mealsToDelete.Contains(me))
                .Concat(generatedMeals)
                .ToList();

            var mealPlanDto = _mapper.Map<MealPlanDto>(existingMealPlan);

            // Invalidate cache AFTER successful operation (PERFORMANCE OPTIMIZATION)
            _ = Task.Run(async () => await InvalidateMealPlanCacheAsync(mealPlanId, userId));

            _logger.LogInformation("Generated {Count} smart meals for meal plan {MealPlanId}", 
                generatedMeals.Count, mealPlanId);

            return response.SetSuccessResponse(mealPlanDto, 
                "Success", $"Successfully generated {generatedMeals.Count} smart meals");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating smart meals for meal plan {MealPlanId}", mealPlanId);
            return response.SetErrorResponse("Error", "Failed to generate smart meals");
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
        var response = new AppResponse<List<MealPlanTemplateDto>>();

        try
        {
            var cacheKey = _cacheKeyService.Custom("templates", "all");
            
            // Try to get from cache first
            var cachedTemplates = await _cacheService.GetAsync<List<MealPlanTemplateDto>>(cacheKey);
            if (cachedTemplates != null)
            {
                return response.SetSuccessResponse(cachedTemplates);
            }

            // For now, return some predefined templates
            // In the future, these could be stored in the database
            var templates = new List<MealPlanTemplateDto>
            {
                new()
                {
                    Id = 1,
                    Name = "Busy Professional Week",
                    Description = "Quick 15-minute meals for busy weekdays",
                    Category = "Quick & Easy",
                    Duration = 7,
                    MealStructure = new Dictionary<string, List<int>>
                    {
                        ["breakfast"] = new List<int> { 101, 102, 103 },
                        ["lunch"] = new List<int> { 201, 202, 203 },
                        ["dinner"] = new List<int> { 301, 302, 303 }
                    }
                }
            };

            // Cache the result
            await _cacheService.SetAsync(cacheKey, templates, TemplatesCacheExpiration);

            return response.SetSuccessResponse(templates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving meal plan templates");
            return response.SetErrorResponse("Error", "Failed to retrieve meal plan templates");
        }
    }

    public async Task<AppResponse<SmartGenerationOptionsDto>> GetSmartGenerationOptionsAsync()
    {
        var response = new AppResponse<SmartGenerationOptionsDto>();

        try
        {
            var cacheKey = _cacheKeyService.Custom("smart-generation", "options");
            
            // Try to get from cache first
            var cachedOptions = await _cacheService.GetAsync<SmartGenerationOptionsDto>(cacheKey);
            if (cachedOptions != null)
            {
                return response.SetSuccessResponse(cachedOptions);
            }

            // Build the available options
            var options = new SmartGenerationOptionsDto
            {
                AvailableCuisineTypes = new List<string>
                {
                    "Italian", "Asian", "Mediterranean", "Mexican", "American", 
                    "French", "Thai", "Indian", "Chinese", "Japanese", 
                    "Korean", "Vietnamese", "Greek", "Spanish", "Middle Eastern"
                },
                BudgetRangeOptions = new List<string> { "low", "medium", "high" },
                DietaryGoalOptions = new List<string> { "balanced", "protein-rich", "low-carb", "high-fiber", "low-sodium" },
                MealComplexityOptions = new List<string> { "simple", "moderate", "complex" },
                MealTypeOptions = new List<string> { "breakfast", "lunch", "dinner", "snack" },
                PlanTypeOptions = new List<string> { "Personal", "Family", "Weekly", "Monthly" },
                FillPatternOptions = new List<string> { "rotate", "random", "same" },
                CookingTimeRange = new CookingTimeRangeDto
                {
                    MinCookingTime = 5,
                    MaxCookingTime = 180,
                    DefaultMaxCookingTime = 45,
                    RecommendedTimeRanges = new List<int> { 15, 30, 45, 60, 90 }
                },
                OptionDescriptions = new Dictionary<string, string>
                {
                    ["budgetRange.low"] = "Budget-friendly recipes with affordable ingredients",
                    ["budgetRange.medium"] = "Balanced cost recipes with quality ingredients",
                    ["budgetRange.high"] = "Premium recipes with high-quality or specialty ingredients",
                    ["mealComplexity.simple"] = "Easy recipes with minimal prep and cooking steps",
                    ["mealComplexity.moderate"] = "Recipes with moderate prep time and cooking techniques",
                    ["mealComplexity.complex"] = "Advanced recipes requiring more time and cooking skills",
                    ["dietaryGoals.balanced"] = "Well-rounded meals with balanced macronutrients",
                    ["dietaryGoals.protein-rich"] = "High-protein meals for muscle building and satiety",
                    ["dietaryGoals.low-carb"] = "Low-carbohydrate meals for weight management",
                    ["dietaryGoals.high-fiber"] = "High-fiber meals for digestive health",
                    ["dietaryGoals.low-sodium"] = "Low-sodium meals for heart health",
                    ["fillPattern.rotate"] = "Cycle through recipes in order across dates",
                    ["fillPattern.random"] = "Randomly select from available recipes",
                    ["fillPattern.same"] = "Use the same recipe for all selected slots",
                    ["includeLeftovers"] = "Include recipes that work well as leftovers for meal prep",
                    ["varietyMode"] = "Ensure variety by avoiding repetitive meals within the plan",
                    ["replaceExisting"] = "Replace existing meals in the plan with new smart-generated ones",
                    ["preserveFavorites"] = "Keep meals marked as favorites and don't replace them"
                }
            };

            // Cache the result for 6 hours
            await _cacheService.SetAsync(cacheKey, options, TimeSpan.FromHours(6));

            return response.SetSuccessResponse(options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving smart generation options");
            return response.SetErrorResponse("Error", "Failed to retrieve smart generation options");
        }
    }

    // PRIVATE METHODS WITH CACHING

    private async Task<List<int>> GetUserAllergiesAsync(int userId)
    {
        try
        {
            var cacheKey = _cacheKeyService.Custom("user", userId, "allergies");
            
            // TRY CACHE FIRST, BUT DON'T BLOCK ON REDIS ISSUES
            try
            {
                var cachedAllergies = await _cacheService.GetAsync<List<int>>(cacheKey);
                if (cachedAllergies != null)
                {
                    return cachedAllergies;
                }
            }
            catch (Exception cacheEx)
            {
                _logger.LogWarning(cacheEx, "Cache access failed for user allergies, proceeding without cache");
            }

            // EXECUTE QUERY DIRECTLY
            var userAllergies = await _unitOfWork.Repository<UserAllergy>()
                .ListAsync(filter: ua => ua.UserId == userId);
            
            var result = userAllergies
                .Where(ua => ua.AllergenId.HasValue)
                .Select(ua => ua.AllergenId.Value)
                .ToList();

            // CACHE RESULT ASYNCHRONOUSLY (DON'T BLOCK)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _cacheService.SetAsync(cacheKey, result, UserAllergiesCacheExpiration);
                }
                catch (Exception cacheSetEx)
                {
                    _logger.LogWarning(cacheSetEx, "Failed to cache user allergies");
                }
            });

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving allergies for user {UserId}", userId);
            return new List<int>();
        }
    }

    private async Task<List<Recipe>> FilterRecipesByPreferencesAsync(MealPlanPreferencesDto preferences, List<int> userAllergies, string mealType)
    {
        try
        {
            // Create cache key based on preferences and allergies
            var preferencesHash = GeneratePreferencesHash(preferences, userAllergies, mealType);
            var cacheKey = _cacheKeyService.Custom("recipes", "filtered", mealType, preferencesHash);

            // TRY CACHE FIRST, BUT DON'T BLOCK ON REDIS ISSUES
            try
            {
                var cachedRecipes = await _cacheService.GetAsync<List<Recipe>>(cacheKey);
                if (cachedRecipes != null && cachedRecipes.Any())
                {
                    return cachedRecipes;
                }
            }
            catch (Exception cacheEx)
            {
                _logger.LogWarning(cacheEx, "Cache access failed for recipe filtering, proceeding without cache");
            }

            // EXECUTE QUERY DIRECTLY
            var filter = MealPlanRecipeQuery.BuildMealPlanFilter(preferences, userAllergies, mealType);
            var orderBy = MealPlanRecipeQuery.BuildMealPlanOrderBy(mealType);
            var includes = MealPlanRecipeQuery.BuildMealPlanIncludes();
            var recipeCount = MealPlanRecipeQuery.GetRecommendedRecipeCount(mealType, preferences);

            var recipes = await _unitOfWork.Repository<Recipe>()
                .ListAsync(
                    filter: filter,
                    orderBy: orderBy,
                    includeProperties: includes
                );

            var result = recipes.Take(recipeCount).ToList();

            // CACHE RESULT ASYNCHRONOUSLY (DON'T BLOCK)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _cacheService.SetAsync(cacheKey, result, RecipeFilterCacheExpiration);
                }
                catch (Exception cacheSetEx)
                {
                    _logger.LogWarning(cacheSetEx, "Failed to cache recipe results");
                }
            });

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error filtering recipes for meal type {MealType}", mealType);
            
            // Fallback to simple query if complex filtering fails
            return await GetFallbackRecipes(mealType);
        }
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

    private bool IsMealFavorite(MealPlanEntry mealEntry)
    {
        // For now, we'll consider meals marked as favorite based on notes
        // In the future, this could be a separate field or table
        return mealEntry.Notes?.Contains("favorite", StringComparison.OrdinalIgnoreCase) == true;
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
