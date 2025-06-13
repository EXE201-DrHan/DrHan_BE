        using DrHan.Application.DTOs.Gemini;
        using DrHan.Application.Interfaces.Repository;
        using DrHan.Application.Interfaces.Services;
using DrHan.Domain.Entities.Ingredients;
        using DrHan.Domain.Entities.Recipes;
        using Microsoft.Extensions.Configuration;
        using Microsoft.Extensions.DependencyInjection;
        using Microsoft.Extensions.Hosting;
        using Microsoft.Extensions.Logging;

        namespace DrHan.Infrastructure.Services;

public class RecipeCacheService : BackgroundService, IRecipeCacheService
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
        // Check if background service is enabled
        var isBackgroundServiceEnabled = _configuration.GetValue<bool>("RecipeCache:EnableBackgroundService", false);
        
        if (!isBackgroundServiceEnabled)
        {
            _logger.LogInformation("Recipe cache background service is disabled in configuration");
            return;
        }

                // Only run if Gemini API key is configured
                if (string.IsNullOrEmpty(_configuration["Gemini:ApiKey"]))
                {
                    _logger.LogInformation("Gemini API key not configured, skipping recipe pre-population");
                    return;
                }

                // Pre-populate database with popular recipes every 24 hours
        var intervalHours = _configuration.GetValue<int>("RecipeCache:IntervalHours", 24);
        var interval = TimeSpan.FromHours(intervalHours);

                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                await PrePopulatePopularRecipesAsync(stoppingToken);
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

    public async Task<int> PrePopulatePopularRecipesAsync(CancellationToken cancellationToken = default)
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
            if (cancellationToken.IsCancellationRequested)
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
                    _logger.LogInformation("Received {Count} recipes from Gemini for '{SearchTerm}'", recipes.Count, searchTerm);
                    
                    // Debug: Log recipe details to see what data we're getting
                    foreach (var recipe in recipes)
                    {
                        _logger.LogDebug("Recipe '{Name}': {IngredientCount} ingredients, {InstructionCount} instructions, {AllergenCount} allergens, {ClaimCount} allergen-free claims", 
                            recipe.Name, 
                            recipe.Ingredients?.Count ?? 0, 
                            recipe.Instructions?.Count ?? 0, 
                            recipe.Allergens?.Count ?? 0, 
                            recipe.AllergenFreeClaims?.Count ?? 0);
                    }
                    
                    var newRecipes = await ConvertAndSaveGeminiRecipes(unitOfWork, recipes, cancellationToken);
                            recipesAdded += newRecipes.Count;
                            
                            _logger.LogInformation("Pre-populated {Count} recipes for '{SearchTerm}'", 
                                newRecipes.Count, searchTerm);
                        }
                else
                {
                    _logger.LogWarning("No recipes received from Gemini for '{SearchTerm}'", searchTerm);
                }

                        // Rate limiting - wait between requests
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
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

        return recipesAdded;
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
                    _logger.LogDebug("Recipe '{Name}' already exists, skipping", geminiRecipe.Name);
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
                await AddRecipeComponents(recipe, geminiRecipe, unitOfWork);

                // Log what we're about to save
                _logger.LogDebug("Saving recipe '{Name}' with {IngredientCount} ingredients, {InstructionCount} instructions, {AllergenCount} allergens, {ClaimCount} allergen-free claims",
                    recipe.Name,
                    recipe.RecipeIngredients.Count,
                    recipe.RecipeInstructions.Count,
                    recipe.RecipeAllergens.Count,
                    recipe.RecipeAllergenFreeClaims.Count);

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
            
            _logger.LogInformation("Successfully saved {Count} recipes to database", newRecipes.Count);
                }

                return newRecipes;
            }

    private async Task AddRecipeComponents(Recipe recipe, GeminiRecipeResponseDto geminiRecipe, IUnitOfWork unitOfWork)
    {
        // Add ingredients with ingredient ID linking
        if (geminiRecipe.Ingredients != null)
        {
            foreach (var ingredient in geminiRecipe.Ingredients)
            {
                // Try to find existing ingredient by name
                var existingIngredient = await FindOrCreateIngredient(ingredient.Name, unitOfWork);

                recipe.RecipeIngredients.Add(new RecipeIngredient
                    {
                        BusinessId = Guid.NewGuid(),
                    Recipe = recipe,
                    IngredientId = existingIngredient?.Id, // Link to existing ingredient if found
                    IngredientName = ingredient.Name, // Keep the name for search/display
                        Quantity = ingredient.Quantity,
                        Unit = ingredient.Unit,
                        PreparationNotes = ingredient.Notes,
                        OrderInRecipe = recipe.RecipeIngredients.Count + 1,
                        CreateAt = DateTime.UtcNow,
                        UpdateAt = DateTime.UtcNow
                    });
            }
                }

                // Add instructions
        if (geminiRecipe.Instructions != null)
        {
                foreach (var instruction in geminiRecipe.Instructions)
                {
                    recipe.RecipeInstructions.Add(new RecipeInstruction
                    {
                        BusinessId = Guid.NewGuid(),
                    Recipe = recipe, // Set the navigation property
                        StepNumber = instruction.StepNumber,
                        InstructionText = instruction.Instruction,
                        TimeMinutes = instruction.EstimatedTimeMinutes,
                        CreateAt = DateTime.UtcNow,
                        UpdateAt = DateTime.UtcNow
                    });
                }
        }

                // Add allergens
        if (geminiRecipe.Allergens != null)
        {
                foreach (var allergen in geminiRecipe.Allergens)
                {
                    recipe.RecipeAllergens.Add(new RecipeAllergen
                    {
                        BusinessId = Guid.NewGuid(),
                    Recipe = recipe, // Set the navigation property
                        AllergenType = allergen,
                        CreateAt = DateTime.UtcNow,
                        UpdateAt = DateTime.UtcNow
                    });
            }
                }

                // Add allergen-free claims
        if (geminiRecipe.AllergenFreeClaims != null)
        {
                foreach (var claim in geminiRecipe.AllergenFreeClaims)
                {
                    recipe.RecipeAllergenFreeClaims.Add(new RecipeAllergenFreeClaim
                    {
                        BusinessId = Guid.NewGuid(),
                    Recipe = recipe, // Set the navigation property
                        Claim = claim,
                        CreateAt = DateTime.UtcNow,
                        UpdateAt = DateTime.UtcNow
                    });
                }
        }
    }

    /// <summary>
    /// Try to find existing ingredient by name, create if not found
    /// </summary>
    private async Task<Ingredient?> FindOrCreateIngredient(string ingredientName, IUnitOfWork unitOfWork)
    {
        try
        {
            // Try to find existing ingredient by exact name match
            var ingredients = await unitOfWork.Repository<Ingredient>().ListAsync(
                filter: i => i.Name.ToLower() == ingredientName.ToLower()
            );

            if (ingredients.Any())
            {
                return ingredients.First();
            }

            // Try to find by similar name (using contains)
            var similarIngredients = await unitOfWork.Repository<Ingredient>().ListAsync(
                filter: i => i.Name.ToLower().Contains(ingredientName.ToLower()) || 
                           ingredientName.ToLower().Contains(i.Name.ToLower())
            );

            if (similarIngredients.Any())
            {
                _logger.LogDebug("Found similar ingredient '{ExistingName}' for '{NewName}'", 
                    similarIngredients.First().Name, ingredientName);
                return similarIngredients.First();
            }

            // Auto-create missing ingredient with smart category matching
            var category = await FindOrCreateIngredientCategory(ingredientName, unitOfWork);
            
            var newIngredient = new Ingredient
            {
                BusinessId = Guid.NewGuid(),
                Name = ingredientName,
                Category = category,
                Description = $"Auto-generated ingredient from AI recipe",
                CreateAt = DateTime.UtcNow,
                UpdateAt = DateTime.UtcNow
            };

            await unitOfWork.Repository<Ingredient>().AddAsync(newIngredient);
            _logger.LogInformation("Created new ingredient: '{IngredientName}' in category '{Category}'", 
                ingredientName, category);
            return newIngredient;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error finding/creating ingredient: {IngredientName}", ingredientName);
            return null;
        }
    }

    /// <summary>
    /// Find appropriate category for ingredient or create new one
    /// </summary>
    private async Task<string> FindOrCreateIngredientCategory(string ingredientName, IUnitOfWork unitOfWork)
    {
        try
        {
            // Get all existing categories
            var existingIngredients = await unitOfWork.Repository<Ingredient>().ListAsync();
            var existingCategories = existingIngredients
                .Where(i => !string.IsNullOrEmpty(i.Category))
                .Select(i => i.Category)
                .Distinct()
                .ToList();

            if (!existingCategories.Any())
            {
                return "AI Generated"; // Fallback if no categories exist
            }

            // Smart category matching based on ingredient name
            var lowerName = ingredientName.ToLower();
            
            // Vietnamese/Asian ingredient patterns
            if (ContainsAny(lowerName, new[] { "nước mắm", "tương", "miso", "sake", "mirin", "kimchi" }))
                return FindBestCategoryMatch(existingCategories, new[] { "gia vị", "sauce", "condiment", "seasoning" });
            
            if (ContainsAny(lowerName, new[] { "thịt", "gà", "vịt", "heo", "bò", "chicken", "beef", "pork", "meat" }))
                return FindBestCategoryMatch(existingCategories, new[] { "thịt", "meat", "protein" });
            
            if (ContainsAny(lowerName, new[] { "cá", "tôm", "cua", "mực", "fish", "shrimp", "seafood" }))
                return FindBestCategoryMatch(existingCategories, new[] { "hải sản", "seafood", "fish" });
            
            if (ContainsAny(lowerName, new[] { "rau", "cải", "salad", "vegetable", "lettuce", "spinach" }))
                return FindBestCategoryMatch(existingCategories, new[] { "rau", "vegetable", "greens" });
            
            if (ContainsAny(lowerName, new[] { "trái", "quả", "fruit", "apple", "banana", "orange" }))
                return FindBestCategoryMatch(existingCategories, new[] { "trái cây", "fruit" });
            
            if (ContainsAny(lowerName, new[] { "gạo", "bánh", "bột", "rice", "flour", "bread", "grain" }))
                return FindBestCategoryMatch(existingCategories, new[] { "tinh bột", "grain", "carbohydrate" });
            
            if (ContainsAny(lowerName, new[] { "sữa", "phô mai", "yogurt", "milk", "cheese", "dairy" }))
                return FindBestCategoryMatch(existingCategories, new[] { "sữa", "dairy" });
            
            if (ContainsAny(lowerName, new[] { "dầu", "mỡ", "oil", "butter", "fat" }))
                return FindBestCategoryMatch(existingCategories, new[] { "dầu mỡ", "oil", "fat" });
            
            if (ContainsAny(lowerName, new[] { "gia vị", "tiêu", "muối", "spice", "pepper", "salt", "herb" }))
                return FindBestCategoryMatch(existingCategories, new[] { "gia vị", "spice", "seasoning" });

            // If no pattern matches, try to find a generic category
            var genericCategory = FindBestCategoryMatch(existingCategories, new[] { "other", "khác", "general", "ai generated" });
            if (!string.IsNullOrEmpty(genericCategory))
                return genericCategory;

            // Create new category based on ingredient type
            return CreateNewCategoryName(ingredientName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error determining category for ingredient: {IngredientName}", ingredientName);
            return "AI Generated";
        }
    }

    private bool ContainsAny(string text, string[] keywords)
    {
        return keywords.Any(keyword => text.Contains(keyword));
    }

    private string FindBestCategoryMatch(List<string> existingCategories, string[] preferredCategories)
    {
        foreach (var preferred in preferredCategories)
        {
            var match = existingCategories.FirstOrDefault(cat => 
                cat.ToLower().Contains(preferred.ToLower()) || preferred.ToLower().Contains(cat.ToLower()));
            if (!string.IsNullOrEmpty(match))
                return match;
        }
        return string.Empty;
    }

    private string CreateNewCategoryName(string ingredientName)
    {
        var lowerName = ingredientName.ToLower();
        
        // Create meaningful category names for Vietnamese ingredients
        if (ContainsAny(lowerName, new[] { "lá", "leaf" }))
            return "Lá gia vị"; // Herb leaves
        
        if (ContainsAny(lowerName, new[] { "hạt", "seed", "nut" }))
            return "Hạt & Đậu"; // Seeds & Nuts
        
        if (ContainsAny(lowerName, new[] { "nước", "liquid", "broth" }))
            return "Nước dùng"; // Liquids/Broths
        
        return "AI Generated"; // Default fallback
            }
        } 