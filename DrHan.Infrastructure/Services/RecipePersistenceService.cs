using DrHan.Application.DTOs.Gemini;
using DrHan.Application.Interfaces.Repository;
using DrHan.Application.Interfaces.Services;
using DrHan.Domain.Entities.Ingredients;
using DrHan.Domain.Entities.Recipes;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace DrHan.Infrastructure.Services;

public class RecipePersistenceService : IRecipePersistenceService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RecipePersistenceService> _logger;
    private readonly ConcurrentQueue<RecipePersistenceItem> _persistenceQueue = new();

    public RecipePersistenceService(
        IServiceProvider serviceProvider,
        ILogger<RecipePersistenceService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task QueueRecipesForPersistenceAsync(List<GeminiRecipeResponseDto> geminiRecipes, string searchContext)
    {
        if (geminiRecipes?.Any() != true)
            return Task.CompletedTask;

        var persistenceItem = new RecipePersistenceItem
        {
            GeminiRecipes = geminiRecipes,
            SearchContext = searchContext,
            QueuedAt = DateTime.UtcNow
        };

        _persistenceQueue.Enqueue(persistenceItem);
        
        _logger.LogInformation("Queued {Count} recipes for background persistence. Search context: {Context}", 
            geminiRecipes.Count, searchContext);

        return Task.CompletedTask;
    }

    public async Task ProcessQueuedRecipesAsync(CancellationToken cancellationToken = default)
    {
        if (_persistenceQueue.IsEmpty)
            return;

        var processedCount = 0;
        var errors = new List<Exception>();

        while (_persistenceQueue.TryDequeue(out var item) && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                await PersistRecipesToDatabase(item, cancellationToken);
                processedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist recipes for search context: {Context}", item.SearchContext);
                errors.Add(ex);
                
                // Re-queue the item for retry (with a limit to prevent infinite loops)
                if (item.RetryCount < 3)
                {
                    item.RetryCount++;
                    _persistenceQueue.Enqueue(item);
                    _logger.LogWarning("Re-queued recipes for retry {RetryCount}/3. Search context: {Context}", 
                        item.RetryCount, item.SearchContext);
                }
            }
        }

        if (processedCount > 0)
        {
            _logger.LogInformation("Successfully processed {ProcessedCount} recipe batches from queue", processedCount);
        }

        if (errors.Any())
        {
            _logger.LogWarning("Encountered {ErrorCount} errors while processing recipe queue", errors.Count);
        }
    }

    private async Task PersistRecipesToDatabase(RecipePersistenceItem item, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var newRecipes = new List<Recipe>();

        foreach (var geminiRecipe in item.GeminiRecipes)
        {
            try
            {
                // Skip if recipe already exists
                var exists = await unitOfWork.Repository<Recipe>()
                    .ExistsAsync(r => r.Name.ToLower() == geminiRecipe.Name.ToLower() &&
                                    r.CuisineType.ToLower() == geminiRecipe.CuisineType.ToLower());

                if (exists)
                {
                    _logger.LogDebug("Recipe {RecipeName} already exists, skipping", geminiRecipe.Name);
                    continue;
                }

                // Convert AI recipe to database recipe
                var recipe = await CreateRecipeFromGeminiDataAsync(geminiRecipe, unitOfWork, cancellationToken);
                newRecipes.Add(recipe);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to convert recipe: {RecipeName}", geminiRecipe.Name);
            }
        }

        // Save all new recipes to database in one transaction
        if (newRecipes.Any())
        {
            await unitOfWork.Repository<Recipe>().AddRangeAsync(newRecipes);
            await unitOfWork.CompleteAsync(cancellationToken);
            
            _logger.LogInformation("Successfully persisted {Count} new AI recipes to database. Search context: {Context}", 
                newRecipes.Count, item.SearchContext);
        }
    }

    private async Task<Recipe> CreateRecipeFromGeminiDataAsync(
        GeminiRecipeResponseDto geminiRecipe, 
        IUnitOfWork unitOfWork, 
        CancellationToken cancellationToken)
    {
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
            SourceUrl = "Được tạo bởi AI",
            OriginalAuthor = "AI Generated",
            CreateAt = DateTime.Now,
            UpdateAt = DateTime.Now
        };

        // Add recipe components
        await AddIngredientsToRecipe(recipe, geminiRecipe.Ingredients, unitOfWork, cancellationToken);
        AddInstructionsToRecipe(recipe, geminiRecipe.Instructions);
        AddAllergensToRecipe(recipe, geminiRecipe.Allergens);
        AddAllergenFreeClaimsToRecipe(recipe, geminiRecipe.AllergenFreeClaims);

        return recipe;
    }

    private async Task AddIngredientsToRecipe(
        Recipe recipe, 
        List<GeminiIngredientDto> ingredients, 
        IUnitOfWork unitOfWork, 
        CancellationToken cancellationToken)
    {
        if (ingredients?.Any() != true)
            return;

        var ingredientNames = ingredients.Select(i => i.Name.ToLower()).ToList();
        var existingIngredients = await unitOfWork.Repository<Ingredient>()
            .ListAsync(i => ingredientNames.Contains(i.Name.ToLower()));

        var existingIngredientDict = existingIngredients.ToDictionary(i => i.Name.ToLower(), i => i);
        var newIngredients = new List<Ingredient>();

        foreach (var ingredient in ingredients)
        {
            try
            {
                var lowerName = ingredient.Name.ToLower();
                Ingredient existingIngredient;

                if (!existingIngredientDict.TryGetValue(lowerName, out existingIngredient))
                {
                    existingIngredient = new Ingredient
                    {
                        BusinessId = Guid.NewGuid(),
                        Name = ingredient.Name,
                        Description = $"Added automatically for recipe: {recipe.Name}",
                        CreateAt = DateTime.Now,
                        UpdateAt = DateTime.Now
                    };
                    newIngredients.Add(existingIngredient);
                    existingIngredientDict[lowerName] = existingIngredient;
                }

                recipe.RecipeIngredients.Add(new RecipeIngredient
                {
                    BusinessId = Guid.NewGuid(),
                    Recipe = recipe,
                    Ingredient = existingIngredient,
                    IngredientName = ingredient.Name,
                    Quantity = ingredient.Quantity,
                    Unit = ingredient.Unit,
                    PreparationNotes = ingredient.Notes,
                    OrderInRecipe = recipe.RecipeIngredients.Count + 1,
                    CreateAt = DateTime.Now,
                    UpdateAt = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding ingredient {IngredientName} to recipe {RecipeName}",
                    ingredient.Name, recipe.Name);
            }
        }

        if (newIngredients.Any())
        {
            await unitOfWork.Repository<Ingredient>().AddRangeAsync(newIngredients);
        }
    }

    private void AddInstructionsToRecipe(Recipe recipe, List<GeminiInstructionDto> instructions)
    {
        if (instructions?.Any() != true)
            return;

        foreach (var instruction in instructions)
        {
            recipe.RecipeInstructions.Add(new RecipeInstruction
            {
                BusinessId = Guid.NewGuid(),
                Recipe = recipe,
                StepNumber = instruction.StepNumber,
                InstructionText = instruction.Instruction,
                TimeMinutes = instruction.EstimatedTimeMinutes,
                CreateAt = DateTime.Now,
                UpdateAt = DateTime.Now
            });
        }
    }

    private void AddAllergensToRecipe(Recipe recipe, List<string> allergens)
    {
        if (allergens?.Any() != true)
            return;

        foreach (var allergen in allergens)
        {
            recipe.RecipeAllergens.Add(new RecipeAllergen
            {
                BusinessId = Guid.NewGuid(),
                Recipe = recipe,
                AllergenType = allergen,
                CreateAt = DateTime.Now,
                UpdateAt = DateTime.Now
            });
        }
    }

    private void AddAllergenFreeClaimsToRecipe(Recipe recipe, List<string> claims)
    {
        if (claims?.Any() != true)
            return;

        foreach (var claim in claims)
        {
            recipe.RecipeAllergenFreeClaims.Add(new RecipeAllergenFreeClaim
            {
                BusinessId = Guid.NewGuid(),
                Recipe = recipe,
                Claim = claim,
                CreateAt = DateTime.Now,
                UpdateAt = DateTime.Now
            });
        }
    }

    private class RecipePersistenceItem
    {
        public List<GeminiRecipeResponseDto> GeminiRecipes { get; set; } = new();
        public string SearchContext { get; set; } = string.Empty;
        public DateTime QueuedAt { get; set; }
        public int RetryCount { get; set; } = 0;
    }
}