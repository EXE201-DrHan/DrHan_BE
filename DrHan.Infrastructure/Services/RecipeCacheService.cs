using DrHan.Application.DTOs.Gemini;
using DrHan.Application.Interfaces.Repository;
using DrHan.Application.Interfaces.Services;
using DrHan.Domain.Entities.Recipes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DrHan.Infrastructure.Services;

public class RecipeCacheService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RecipeCacheService> _logger;
    private readonly IConfiguration _configuration;

    public RecipeCacheService(
        IServiceProvider serviceProvider,
        ILogger<RecipeCacheService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Only run if Gemini API key is configured
        if (string.IsNullOrEmpty(_configuration["Gemini:ApiKey"]))
        {
            _logger.LogInformation("Gemini API key not configured, skipping recipe pre-population");
            return;
        }

        // Pre-populate database with popular recipes every 24 hours
        var interval = TimeSpan.FromHours(24);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PrePopulatePopularRecipes(stoppingToken);
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Recipe cache service cancelled");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in recipe cache service");
                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
            }
        }
    }

    private async Task PrePopulatePopularRecipes(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var geminiService = scope.ServiceProvider.GetRequiredService<IGeminiRecipeService>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var popularSearchTerms = new[]
        {
            "chicken recipes", "pasta recipes", "vegetarian recipes",
            "quick dinner", "healthy breakfast", "dessert recipes",
            "soup recipes", "salad recipes", "beef recipes", "fish recipes"
        };

        var recipesAdded = 0;

        foreach (var searchTerm in popularSearchTerms)
        {
            if (stoppingToken.IsCancellationRequested)
                break;

            try
            {
                // Check if we already have recipes for this search term
                var existingCount = await CheckExistingRecipesCount(unitOfWork, searchTerm);
                if (existingCount >= 3) // Skip if we already have enough recipes for this term
                {
                    _logger.LogDebug("Skipping '{SearchTerm}' - already have {Count} recipes", searchTerm, existingCount);
                    continue;
                }

                var request = new GeminiRecipeRequestDto
                {
                    SearchQuery = searchTerm,
                    Count = 3 - existingCount
                };

                var recipes = await geminiService.SearchRecipesAsync(request);
                
                if (recipes.Any())
                {
                    var newRecipes = await ConvertAndSaveGeminiRecipes(unitOfWork, recipes, stoppingToken);
                    recipesAdded += newRecipes.Count;
                    
                    _logger.LogInformation("Pre-populated {Count} recipes for '{SearchTerm}'", 
                        newRecipes.Count, searchTerm);
                }

                // Rate limiting - wait between requests
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pre-populating recipes for term: {SearchTerm}", searchTerm);
            }
        }

        if (recipesAdded > 0)
        {
            _logger.LogInformation("Recipe pre-population completed. Added {TotalRecipes} new recipes", recipesAdded);
        }
        else
        {
            _logger.LogInformation("Recipe pre-population completed. No new recipes added");
        }
    }

    private async Task<int> CheckExistingRecipesCount(IUnitOfWork unitOfWork, string searchTerm)
    {
        var recipes = await unitOfWork.Repository<Recipe>().ListAsync(
            filter: r => r.Name.Contains(searchTerm) || r.Description.Contains(searchTerm)
        );
        return recipes.Count;
    }

    private async Task<List<Recipe>> ConvertAndSaveGeminiRecipes(
        IUnitOfWork unitOfWork,
        List<GeminiRecipeResponseDto> geminiRecipes,
        CancellationToken cancellationToken)
    {
        var newRecipes = new List<Recipe>();

        foreach (var geminiRecipe in geminiRecipes)
        {
            try
            {
                // Check if recipe already exists
                var exists = await unitOfWork.Repository<Recipe>()
                    .ExistsAsync(r => r.Name.ToLower() == geminiRecipe.Name.ToLower() && 
                                     r.CuisineType.ToLower() == geminiRecipe.CuisineType.ToLower());

                if (exists)
                {
                    continue;
                }

                var recipe = new Recipe
                {
                    BusinessId = Guid.NewGuid(),
                    Name = geminiRecipe.Name,
                    Description = geminiRecipe.Description,
                    CuisineType = geminiRecipe.CuisineType,
                    MealType = geminiRecipe.MealType,
                    PrepTimeMinutes = geminiRecipe.PrepTimeMinutes,
                    CookTimeMinutes = geminiRecipe.CookTimeMinutes,
                    Servings = geminiRecipe.Servings,
                    DifficultyLevel = geminiRecipe.DifficultyLevel,
                    IsCustom = false,
                    IsPublic = true,
                    SourceUrl = "Generated by AI",
                    OriginalAuthor = "AI Generated",
                    CreateAt = DateTime.UtcNow,
                    UpdateAt = DateTime.UtcNow
                };

                // Add components (ingredients, instructions, etc.)
                AddRecipeComponents(recipe, geminiRecipe);

                newRecipes.Add(recipe);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting Gemini recipe: {RecipeName}", geminiRecipe.Name);
            }
        }

        if (newRecipes.Any())
        {
            await unitOfWork.Repository<Recipe>().AddRangeAsync(newRecipes);
            await unitOfWork.CompleteAsync(cancellationToken);
        }

        return newRecipes;
    }

    private static void AddRecipeComponents(Recipe recipe, GeminiRecipeResponseDto geminiRecipe)
    {
        // Add ingredients
        foreach (var ingredient in geminiRecipe.Ingredients)
        {
            recipe.RecipeIngredients.Add(new RecipeIngredient
            {
                BusinessId = Guid.NewGuid(),
                IngredientName = ingredient.Name,
                Quantity = ingredient.Quantity,
                Unit = ingredient.Unit,
                PreparationNotes = ingredient.Notes,
                OrderInRecipe = recipe.RecipeIngredients.Count + 1,
                CreateAt = DateTime.UtcNow,
                UpdateAt = DateTime.UtcNow
            });
        }

        // Add instructions
        foreach (var instruction in geminiRecipe.Instructions)
        {
            recipe.RecipeInstructions.Add(new RecipeInstruction
            {
                BusinessId = Guid.NewGuid(),
                StepNumber = instruction.StepNumber,
                InstructionText = instruction.Instruction,
                TimeMinutes = instruction.EstimatedTimeMinutes,
                CreateAt = DateTime.UtcNow,
                UpdateAt = DateTime.UtcNow
            });
        }

        // Add nutrition
        foreach (var nutrition in geminiRecipe.Nutrition)
        {
            recipe.RecipeNutritions.Add(new RecipeNutrition
            {
                BusinessId = Guid.NewGuid(),
                NutrientName = nutrition.NutrientName,
                AmountPerServing = nutrition.Amount,
                Unit = nutrition.Unit,
                DailyValuePercent = nutrition.DailyValuePercentage,
                CreateAt = DateTime.UtcNow,
                UpdateAt = DateTime.UtcNow
            });
        }

        // Add allergens
        foreach (var allergen in geminiRecipe.Allergens)
        {
            recipe.RecipeAllergens.Add(new RecipeAllergen
            {
                BusinessId = Guid.NewGuid(),
                AllergenType = allergen,
                CreateAt = DateTime.UtcNow,
                UpdateAt = DateTime.UtcNow
            });
        }

        // Add allergen-free claims
        foreach (var claim in geminiRecipe.AllergenFreeClaims)
        {
            recipe.RecipeAllergenFreeClaims.Add(new RecipeAllergenFreeClaim
            {
                BusinessId = Guid.NewGuid(),
                Claim = claim,
                CreateAt = DateTime.UtcNow,
                UpdateAt = DateTime.UtcNow
            });
        }
    }
} 