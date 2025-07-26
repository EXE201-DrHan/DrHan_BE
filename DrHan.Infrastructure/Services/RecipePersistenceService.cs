using DrHan.Application.DTOs.Gemini;
using DrHan.Application.Interfaces.Repository;
using DrHan.Application.Interfaces.Services;
using DrHan.Domain.Entities.Ingredients;
using DrHan.Domain.Entities.Recipes;
using Microsoft.Extensions.DependencyInjection;
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

    /// <summary>
    /// FIXED: Use async disposal and comprehensive duplicate handling
    /// </summary>
    private async Task PersistRecipesToDatabase(RecipePersistenceItem item, CancellationToken cancellationToken)
    {
        // FIX 1: Use AsyncServiceScope instead of regular ServiceScope for proper async disposal
        await using var scope = _serviceProvider.CreateAsyncScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        try
        {
            var newRecipes = new List<Recipe>();

            // Pre-process: Get existing ingredient names to avoid conflicts
            var allIngredientNames = item.GeminiRecipes
                .SelectMany(gr => gr.Ingredients ?? new List<GeminiIngredientDto>())
                .Select(i => i.Name.ToLower().Trim())
                .Distinct()
                .ToList();

            var existingIngredients = new Dictionary<string, Ingredient>();
            if (allIngredientNames.Any())
            {
                var existingIngredientsFromDb = await unitOfWork.Repository<Ingredient>()
                    .ListAsync(i => allIngredientNames.Contains(i.Name.ToLower()));

                existingIngredients = existingIngredientsFromDb
                    .ToDictionary(i => i.Name.ToLower().Trim(), i => i);

                _logger.LogDebug("Pre-loaded {Count} existing ingredients", existingIngredients.Count);
            }

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

                    // Convert AI recipe to database recipe with pre-loaded ingredients
                    var recipe = await CreateRecipeFromGeminiDataAsync(
                        geminiRecipe,
                        unitOfWork,
                        existingIngredients,
                        cancellationToken);

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
                await SaveRecipesWithDuplicateHandling(newRecipes, unitOfWork, item.SearchContext, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist recipes batch for context: {Context}", item.SearchContext);
            throw;
        }
        // Note: AsyncServiceScope will handle disposal automatically
    }

    /// <summary>
    /// Save recipes with comprehensive duplicate ingredient handling
    /// </summary>
    private async Task SaveRecipesWithDuplicateHandling(
        List<Recipe> recipes,
        IUnitOfWork unitOfWork,
        string searchContext,
        CancellationToken cancellationToken)
    {
        try
        {
            // First attempt: Save everything in batch
            await unitOfWork.Repository<Recipe>().AddRangeAsync(recipes);
            await unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("✅ Successfully persisted {Count} new AI recipes to database. Search context: {Context}",
                recipes.Count, searchContext);
        }
        catch (Exception ex) when (IsDuplicateKeyError(ex))
        {
            _logger.LogWarning("⚠️ Duplicate ingredient detected during batch save, switching to individual processing");

            // Clear the context to start fresh
            unitOfWork.DetachAllEntities();

            // Save recipes individually with duplicate handling
            await SaveRecipesIndividually(recipes, unitOfWork, searchContext, cancellationToken);
        }
    }

    /// <summary>
    /// Save recipes one by one with individual error handling
    /// </summary>
    private async Task SaveRecipesIndividually(
        List<Recipe> recipes,
        IUnitOfWork unitOfWork,
        string searchContext,
        CancellationToken cancellationToken)
    {
        var successCount = 0;
        var failedRecipes = new List<string>();

        foreach (var recipe in recipes)
        {
            try
            {
                // Resolve any ingredient conflicts for this specific recipe
                await ResolveIngredientConflicts(recipe, unitOfWork, cancellationToken);

                // Try to save this individual recipe
                await unitOfWork.Repository<Recipe>().AddAsync(recipe);
                await unitOfWork.CompleteAsync(cancellationToken);

                successCount++;
                _logger.LogDebug("✅ Saved recipe: {RecipeName}", recipe.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to save individual recipe: {RecipeName}", recipe.Name);
                failedRecipes.Add(recipe.Name);

                // Detach the problematic entity to continue with others
                unitOfWork.DetachEntity(recipe);
            }
        }

        _logger.LogInformation("📊 Individual save results for context {Context}: {Success}/{Total} successful, {Failed} failed",
            searchContext, successCount, recipes.Count, failedRecipes.Count);

        if (failedRecipes.Any())
        {
            _logger.LogWarning("❌ Failed recipes: {FailedRecipes}", string.Join(", ", failedRecipes));
        }
    }

    /// <summary>
    /// Resolve ingredient conflicts for a specific recipe
    /// </summary>
    private async Task ResolveIngredientConflicts(
        Recipe recipe,
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        if (!recipe.RecipeIngredients.Any())
            return;

        var ingredientNames = recipe.RecipeIngredients
            .Select(ri => ri.IngredientName.ToLower().Trim())
            .Distinct()
            .ToList();

        // Get the latest state of ingredients from database
        var currentIngredients = await unitOfWork.Repository<Ingredient>()
            .ListAsync(i => ingredientNames.Contains(i.Name.ToLower()));

        var ingredientDict = currentIngredients.ToDictionary(i => i.Name.ToLower().Trim(), i => i);

        // Update recipe ingredients to use existing ingredients
        foreach (var recipeIngredient in recipe.RecipeIngredients.ToList())
        {
            var lowerName = recipeIngredient.IngredientName.ToLower().Trim();

            if (ingredientDict.TryGetValue(lowerName, out var existingIngredient))
            {
                // Use existing ingredient
                recipeIngredient.Ingredient = existingIngredient;
            }
            else
            {
                // Try to create new ingredient safely
                try
                {
                    var newIngredient = await CreateIngredientSafely(
                        recipeIngredient.IngredientName,
                        recipe.Name,
                        unitOfWork,
                        cancellationToken);

                    if (newIngredient != null)
                    {
                        recipeIngredient.Ingredient = newIngredient;
                        ingredientDict[lowerName] = newIngredient; // Cache for other ingredients in same recipe
                    }
                    else
                    {
                        // Remove ingredient if we can't create it
                        _logger.LogWarning("⚠️ Removing ingredient {IngredientName} from recipe {RecipeName} - could not resolve",
                            recipeIngredient.IngredientName, recipe.Name);
                        recipe.RecipeIngredients.Remove(recipeIngredient);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error resolving ingredient {IngredientName} for recipe {RecipeName}",
                        recipeIngredient.IngredientName, recipe.Name);
                    recipe.RecipeIngredients.Remove(recipeIngredient);
                }
            }
        }
    }

    /// <summary>
    /// Safely create an ingredient with duplicate handling
    /// </summary>
    private async Task<Ingredient?> CreateIngredientSafely(
        string ingredientName,
        string recipeName,
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var lowerName = ingredientName.ToLower().Trim();

        try
        {
            // Double-check if ingredient was created by another process
            var existing = await unitOfWork.Repository<Ingredient>()
                .FindAsync(i => i.Name.ToLower() == lowerName);

            if (existing != null)
            {
                _logger.LogDebug("🔄 Ingredient {IngredientName} found in database during conflict resolution", ingredientName);
                return existing;
            }

            // Create new ingredient
            var newIngredient = new Ingredient
            {
                BusinessId = Guid.NewGuid(),
                Name = ingredientName,
                Description = $"Added automatically for recipe: {recipeName}",
                CreateAt = DateTime.Now,
                UpdateAt = DateTime.Now
            };

            await unitOfWork.Repository<Ingredient>().AddAsync(newIngredient);
            await unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogDebug("✅ Created new ingredient: {IngredientName}", ingredientName);
            return newIngredient;
        }
        catch (Exception ex) when (IsDuplicateKeyError(ex))
        {
            _logger.LogDebug("🔄 Ingredient {IngredientName} was created by another process, fetching from database", ingredientName);

            // Detach any problematic entities
            unitOfWork.DetachAllEntities();

            // Another process created this ingredient, fetch it
            var fetchedIngredient = await unitOfWork.Repository<Ingredient>()
                .FindAsync(i => i.Name.ToLower() == lowerName);

            return fetchedIngredient;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Unexpected error creating ingredient {IngredientName}", ingredientName);
            return null;
        }
    }

    /// <summary>
    /// Check if exception is related to duplicate key constraint
    /// </summary>
    private static bool IsDuplicateKeyError(Exception ex)
    {
        var message = ex.Message.ToLower();
        return message.Contains("duplicate key") ||
               message.Contains("ix_ingredients_name") ||
               message.Contains("unique index") ||
               message.Contains("unique constraint");
    }

    private async Task<Recipe> CreateRecipeFromGeminiDataAsync(
        GeminiRecipeResponseDto geminiRecipe,
        IUnitOfWork unitOfWork,
        Dictionary<string, Ingredient> existingIngredients,
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

        // Add recipe components with pre-loaded ingredients
        await AddIngredientsToRecipe(recipe, geminiRecipe.Ingredients, existingIngredients);
        AddInstructionsToRecipe(recipe, geminiRecipe.Instructions);
        AddAllergensToRecipe(recipe, geminiRecipe.Allergens);
        AddAllergenFreeClaimsToRecipe(recipe, geminiRecipe.AllergenFreeClaims);

        return recipe;
    }

    /// <summary>
    /// OPTIMIZED: Add ingredients using pre-loaded ingredient dictionary
    /// </summary>
    private async Task AddIngredientsToRecipe(
        Recipe recipe,
        List<GeminiIngredientDto> ingredients,
        Dictionary<string, Ingredient> existingIngredients)
    {
        if (ingredients?.Any() != true)
            return;

        var newIngredientsNeeded = new Dictionary<string, Ingredient>();

        foreach (var ingredient in ingredients)
        {
            try
            {
                var lowerName = ingredient.Name.ToLower().Trim();
                Ingredient ingredientToUse;

                if (existingIngredients.TryGetValue(lowerName, out var existingIngredient))
                {
                    // Use existing ingredient
                    ingredientToUse = existingIngredient;
                }
                else if (newIngredientsNeeded.TryGetValue(lowerName, out var newIngredient))
                {
                    // Use already created new ingredient
                    ingredientToUse = newIngredient;
                }
                else
                {
                    // Create new ingredient (will be saved with the recipe)
                    ingredientToUse = new Ingredient
                    {
                        BusinessId = Guid.NewGuid(),
                        Name = ingredient.Name,
                        Description = $"Added automatically for recipe: {recipe.Name}",
                        CreateAt = DateTime.Now,
                        UpdateAt = DateTime.Now
                    };

                    newIngredientsNeeded[lowerName] = ingredientToUse;
                    existingIngredients[lowerName] = ingredientToUse; // Add to existing for future use
                }

                recipe.RecipeIngredients.Add(new RecipeIngredient
                {
                    BusinessId = Guid.NewGuid(),
                    Recipe = recipe,
                    Ingredient = ingredientToUse,
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