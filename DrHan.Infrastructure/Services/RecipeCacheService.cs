using DrHan.Application.DTOs.Gemini;
using DrHan.Application.Interfaces.Repository;
using DrHan.Application.Interfaces.Services;
using DrHan.Domain.Entities;
using DrHan.Domain.Entities.Ingredients;
using DrHan.Domain.Entities.Recipes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace DrHan.Infrastructure.Services;

public static class RepositoryExtensions
{
    public static async Task<T?> FirstOrDefaultAsync<T>(this IGenericRepository<T> repository, Expression<Func<T, bool>> filter) where T : BaseEntity
    {
        var items = await repository.ListAsync(filter);
        return items.FirstOrDefault();
    }
}

public class RecipeCacheService : BackgroundService, IRecipeCacheService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RecipeCacheService> _logger;
    private readonly IConfiguration _configuration;
    private readonly TimeSpan _defaultRateLimitDelay = TimeSpan.FromSeconds(2);
    private readonly TimeSpan _errorRetryDelay = TimeSpan.FromMinutes(30);

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
            _logger.LogError("Gemini API key not configured. Please set the 'Gemini:ApiKey' configuration value.");
            return;
        }

        // Pre-populate database with popular recipes every 24 hours
        var intervalHours = _configuration.GetValue<int>("RecipeCache:IntervalHours", 24);
        var interval = TimeSpan.FromHours(intervalHours);

        _logger.LogInformation("Recipe cache service started. Will run every {Hours} hours", intervalHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Starting recipe cache population cycle");
                await PrePopulatePopularRecipesAsync(null, stoppingToken);
                _logger.LogInformation("Recipe cache population cycle completed. Next run in {Hours} hours", intervalHours);
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
                await Task.Delay(_errorRetryDelay, stoppingToken);
            }
        }
    }

    public async Task<int> PrePopulatePopularRecipesAsync(GeminiRecipeRequestDto? request = null, CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var geminiService = scope.ServiceProvider.GetRequiredService<IGeminiRecipeService>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var popularSearchTerms = new[]
        {
            "món ăn Việt Nam", "món ăn châu Á", "món ăn nhanh",
            "món ăn healthy", "món ăn chay", "món tráng miệng",
            "món canh", "món salad", "món thịt", "món hải sản"
        };

        var recipesAdded = 0;
        var errors = 0;
        var rateLimitDelay = _defaultRateLimitDelay;

        foreach (var searchTerm in popularSearchTerms)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("Recipe population cancelled");
                break;
            }

            try
            {
                // Check if we already have recipes for this search term
                var existingCount = await CheckExistingRecipesCount(unitOfWork, searchTerm);
                if (existingCount >= 5) // Increased from 3 to 5 to match search logic
                {
                    _logger.LogDebug("Skipping '{SearchTerm}' - already have {Count} recipes", searchTerm, existingCount);
                    continue;
                }

                var recipeRequest = request ?? new GeminiRecipeRequestDto
                {
                    SearchQuery = searchTerm,
                    Count = 5 - existingCount, // Request up to 5 recipes per term
                    CuisineType = searchTerm.Contains("Việt Nam") ? "Vietnamese" : 
                                 searchTerm.Contains("châu Á") ? "Asian" : null,
                    MealType = searchTerm.Contains("tráng miệng") ? "Dessert" :
                              searchTerm.Contains("canh") ? "Soup" :
                              searchTerm.Contains("salad") ? "Salad" : null,
                    IncludeImage = true // Request recipe images
                };

                _logger.LogInformation("Requesting {Count} recipes for '{SearchTerm}'", recipeRequest.Count, searchTerm);
                var recipes = await geminiService.SearchRecipesAsync(recipeRequest);
                
                if (recipes.Any())
                {
                    _logger.LogInformation("Received {Count} recipes from Gemini for '{SearchTerm}'", recipes.Count, searchTerm);
                    
                    // Debug: Log recipe details
                    foreach (var recipe in recipes)
                    {
                        _logger.LogDebug("Recipe '{Name}': {IngredientCount} ingredients, {InstructionCount} instructions, {AllergenCount} allergens, {ClaimCount} allergen-free claims, HasImage: {HasImage}", 
                            recipe.Name, 
                            recipe.Ingredients?.Count ?? 0, 
                            recipe.Instructions?.Count ?? 0, 
                            recipe.Allergens?.Count ?? 0, 
                            recipe.AllergenFreeClaims?.Count ?? 0,
                            !string.IsNullOrEmpty(recipe.ImageUrl));
                    }
                    
                    var newRecipes = await ConvertAndSaveGeminiRecipes(recipes, searchTerm);
                    recipesAdded += newRecipes.Count;
                    
                    _logger.LogInformation("Pre-populated {Count} recipes for '{SearchTerm}'", 
                        newRecipes.Count, searchTerm);
                }
                else
                {
                    _logger.LogWarning("No recipes received from Gemini for '{SearchTerm}'", searchTerm);
                }

                // Rate limiting - wait between requests
                await Task.Delay(rateLimitDelay, cancellationToken);
            }
            catch (Exception ex)
            {
                errors++;
                _logger.LogError(ex, "Error pre-populating recipes for term: {SearchTerm}", searchTerm);
                // Increase delay on error to avoid overwhelming the API
                rateLimitDelay = TimeSpan.FromSeconds(Math.Min(rateLimitDelay.TotalSeconds * 2, 30));
            }
        }

        if (recipesAdded > 0)
        {
            _logger.LogInformation("Recipe pre-population completed. Added {TotalRecipes} new recipes with {Errors} errors", 
                recipesAdded, errors);
        }
        else
        {
            _logger.LogInformation("Recipe pre-population completed. No new recipes added. Encountered {Errors} errors", errors);
        }

        return recipesAdded;
    }

    private async Task<int> CheckExistingRecipesCount(IUnitOfWork unitOfWork, string searchTerm)
    {
        var recipes = await unitOfWork.Repository<Recipe>().ListAsync(
            filter: r => r.Name.Contains(searchTerm) || r.Description.Contains(searchTerm) ||
                        r.RecipeIngredients.Any(ri => ri.IngredientName.Contains(searchTerm))
        );
        return recipes.Count;
    }

    private async Task<List<Recipe>> ConvertAndSaveGeminiRecipes(List<GeminiRecipeResponseDto> recipes, string searchTerm)
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        
        var newRecipes = new List<Recipe>();
        var errors = new List<(string RecipeName, string Error)>();

        foreach (var geminiRecipe in recipes)
        {
            try
            {
                // Validate recipe data first
                if (!ValidateRecipeData(geminiRecipe))
                {
                    errors.Add((geminiRecipe.Name, "Invalid recipe data"));
                    continue;
                }

                // Check if recipe already exists
                var exists = await unitOfWork.Repository<Recipe>()
                    .ExistsAsync(r => r.Name.ToLower() == geminiRecipe.Name.ToLower() && 
                                    r.CuisineType.ToLower() == geminiRecipe.CuisineType.ToLower());

                if (exists)
                {
                    _logger.LogDebug("Recipe '{Name}' already exists, skipping", geminiRecipe.Name);
                    continue;
                }

                _logger.LogInformation("Converting and saving recipe: {RecipeName}", geminiRecipe.Name);

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

                // Add recipe image if available and valid
                var validImageUrl = ValidateAndCleanImageUrl(geminiRecipe.ImageUrl);
                if (!string.IsNullOrEmpty(validImageUrl))
                {
                    recipe.RecipeImages.Add(new RecipeImage 
                    { 
                        BusinessId = Guid.NewGuid(),
                        ImageUrl = validImageUrl,
                        CreateAt = DateTime.UtcNow,
                        UpdateAt = DateTime.UtcNow
                    });
                    _logger.LogDebug("Added valid image URL for recipe '{Name}': {ImageUrl}", recipe.Name, validImageUrl);
                }
                else if (!string.IsNullOrEmpty(geminiRecipe.ImageUrl))
                {
                    _logger.LogWarning("Invalid or too long image URL for recipe '{Name}', skipping image", recipe.Name);
                }

                // Add all recipe components using the existing method
                await AddRecipeComponents(recipe, geminiRecipe, unitOfWork);

                // Validate recipe components
                if (!ValidateRecipeComponents(recipe))
                {
                    errors.Add((recipe.Name, "Invalid recipe components"));
                    continue;
                }

                // Log what we're about to save
                _logger.LogDebug("Saving recipe '{Name}' with {IngredientCount} ingredients, {InstructionCount} instructions, {AllergenCount} allergens, {ClaimCount} allergen-free claims, HasImage: {HasImage}",
                    recipe.Name,
                    recipe.RecipeIngredients.Count,
                    recipe.RecipeInstructions.Count,
                    recipe.RecipeAllergens.Count,
                    recipe.RecipeAllergenFreeClaims.Count,
                    recipe.RecipeImages.Any());

                newRecipes.Add(recipe);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting Gemini recipe: {RecipeName}", geminiRecipe.Name);
                errors.Add((geminiRecipe.Name, ex.Message));
            }
        }

        // Save all recipes in a single transaction
        if (newRecipes.Any())
        {
            try
            {
                await unitOfWork.BeginTransactionAsync();
                await unitOfWork.Repository<Recipe>().AddRangeAsync(newRecipes);
                await unitOfWork.CompleteAsync();
                await unitOfWork.CommitTransactionAsync();
                _logger.LogInformation("Successfully saved {Count} recipes to database", newRecipes.Count);
            }
            catch (Exception ex)
            {
                await unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Failed to save recipes to database");
                throw;
            }
        }

        // Log any errors that occurred during conversion
        if (errors.Any())
        {
            _logger.LogWarning("Encountered {ErrorCount} errors during recipe conversion:", errors.Count);
            foreach (var (recipeName, error) in errors)
            {
                _logger.LogWarning("Recipe '{Name}': {Error}", recipeName, error);
            }
        }

        return newRecipes;
    }

    private bool ValidateRecipeData(GeminiRecipeResponseDto recipe)
    {
        if (string.IsNullOrWhiteSpace(recipe.Name) || 
            string.IsNullOrWhiteSpace(recipe.Description) ||
            string.IsNullOrWhiteSpace(recipe.CuisineType) ||
            string.IsNullOrWhiteSpace(recipe.MealType))
        {
            _logger.LogWarning("Recipe validation failed: Missing required fields for recipe '{Name}'", recipe.Name);
            return false;
        }

        if (recipe.PrepTimeMinutes < 0 || recipe.CookTimeMinutes < 0 || recipe.Servings <= 0)
        {
            _logger.LogWarning("Recipe validation failed: Invalid time or servings for recipe '{Name}'", recipe.Name);
            return false;
        }

        if (recipe.Ingredients == null || !recipe.Ingredients.Any())
        {
            _logger.LogWarning("Recipe validation failed: No ingredients for recipe '{Name}'", recipe.Name);
            return false;
        }

        if (recipe.Instructions == null || !recipe.Instructions.Any())
        {
            _logger.LogWarning("Recipe validation failed: No instructions for recipe '{Name}'", recipe.Name);
            return false;
        }

        return true;
    }

    private bool ValidateRecipeComponents(Recipe recipe)
    {
        if (!recipe.RecipeIngredients.Any() || !recipe.RecipeInstructions.Any())
        {
            _logger.LogWarning("Recipe component validation failed: Missing ingredients or instructions for recipe '{Name}'", recipe.Name);
            return false;
        }

        // Validate instruction step numbers
        var stepNumbers = recipe.RecipeInstructions.Select(i => i.StepNumber).ToList();
        if (stepNumbers.Distinct().Count() != stepNumbers.Count)
        {
            _logger.LogWarning("Recipe component validation failed: Duplicate step numbers in recipe '{Name}'", recipe.Name);
            return false;
        }

        // Validate ingredient quantities
        if (recipe.RecipeIngredients.Any(i => i.Quantity <= 0))
        {
            _logger.LogWarning("Recipe component validation failed: Invalid ingredient quantities in recipe '{Name}'", recipe.Name);
            return false;
        }

        return true;
    }

    private async Task AddRecipeComponents(Recipe recipe, GeminiRecipeResponseDto geminiRecipe, IUnitOfWork unitOfWork)
    {
        // Add ingredients with ingredient ID linking
        if (geminiRecipe.Ingredients != null)
        {
            // Batch process ingredients to avoid multiple database round trips
            var ingredientNames = geminiRecipe.Ingredients.Select(i => i.Name.ToLower()).ToList();
            var existingIngredients = await unitOfWork.Repository<Ingredient>()
                .ListAsync(i => ingredientNames.Contains(i.Name.ToLower()));

            var existingIngredientDict = existingIngredients.ToDictionary(i => i.Name.ToLower(), i => i);
            var newIngredients = new List<Ingredient>();

            foreach (var ingredient in geminiRecipe.Ingredients)
            {
                try
                {
                    var lowerName = ingredient.Name.ToLower();
                    Ingredient existingIngredient;

                    if (!existingIngredientDict.TryGetValue(lowerName, out existingIngredient))
                    {
                        // Create new ingredient if it doesn't exist
                        existingIngredient = new Ingredient
                        {
                            BusinessId = Guid.NewGuid(),
                            Name = ingredient.Name,
                            Category = await FindOrCreateIngredientCategory(ingredient.Name, unitOfWork),
                            Description = $"Added automatically for recipe: {recipe.Name}",
                            CreateAt = DateTime.UtcNow,
                            UpdateAt = DateTime.UtcNow
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
                        CreateAt = DateTime.UtcNow,
                        UpdateAt = DateTime.UtcNow
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error adding ingredient {IngredientName} to recipe {RecipeName}",
                        ingredient.Name, recipe.Name);
                }
            }

            // Add all new ingredients at once
            if (newIngredients.Any())
            {
                await unitOfWork.Repository<Ingredient>().AddRangeAsync(newIngredients);
            }
        }

        // Add instructions
        if (geminiRecipe.Instructions != null)
        {
            foreach (var instruction in geminiRecipe.Instructions)
            {
                try
                {
                    recipe.RecipeInstructions.Add(new RecipeInstruction
                    {
                        BusinessId = Guid.NewGuid(),
                        Recipe = recipe,
                        StepNumber = instruction.StepNumber,
                        InstructionText = instruction.Instruction,
                        TimeMinutes = instruction.EstimatedTimeMinutes,
                        CreateAt = DateTime.UtcNow,
                        UpdateAt = DateTime.UtcNow
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error adding instruction step {StepNumber} to recipe {RecipeName}",
                        instruction.StepNumber, recipe.Name);
                }
            }
        }

        // Add allergens
        if (geminiRecipe.Allergens != null)
        {
            foreach (var allergen in geminiRecipe.Allergens)
            {
                try
                {
                    recipe.RecipeAllergens.Add(new RecipeAllergen
                    {
                        BusinessId = Guid.NewGuid(),
                        Recipe = recipe,
                        AllergenType = allergen,
                        CreateAt = DateTime.UtcNow,
                        UpdateAt = DateTime.UtcNow
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error adding allergen {Allergen} to recipe {RecipeName}",
                        allergen, recipe.Name);
                }
            }
        }

        // Add allergen-free claims
        if (geminiRecipe.AllergenFreeClaims != null)
        {
            foreach (var claim in geminiRecipe.AllergenFreeClaims)
            {
                try
                {
                    recipe.RecipeAllergenFreeClaims.Add(new RecipeAllergenFreeClaim
                    {
                        BusinessId = Guid.NewGuid(),
                        Recipe = recipe,
                        Claim = claim,
                        CreateAt = DateTime.UtcNow,
                        UpdateAt = DateTime.UtcNow
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error adding allergen-free claim {Claim} to recipe {RecipeName}",
                        claim, recipe.Name);
                }
            }
        }
    }

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
            if (ContainsAny(lowerName, new[] { "nước mắm", "tương", "miso", "sake", "mirin", "kimchi", "dầu hào", "dầu mè", "dầu điều" }))
                return FindBestCategoryMatch(existingCategories, new[] { "gia vị", "sauce", "condiment", "seasoning" });
            
            if (ContainsAny(lowerName, new[] { "thịt", "gà", "vịt", "heo", "bò", "chicken", "beef", "pork", "meat", "lợn", "trâu" }))
                return FindBestCategoryMatch(existingCategories, new[] { "thịt", "meat", "protein" });
            
            if (ContainsAny(lowerName, new[] { "cá", "tôm", "cua", "mực", "fish", "shrimp", "seafood", "hải sản", "tôm hùm", "sò điệp" }))
                return FindBestCategoryMatch(existingCategories, new[] { "hải sản", "seafood", "fish" });
            
            if (ContainsAny(lowerName, new[] { "rau", "cải", "salad", "vegetable", "lettuce", "spinach", "xà lách", "rau muống", "rau cải" }))
                return FindBestCategoryMatch(existingCategories, new[] { "rau", "vegetable", "greens" });

            if (ContainsAny(lowerName, new[] { "gạo", "nếp", "rice", "bún", "phở", "mì", "noodle", "bánh phở", "bánh canh" }))
                return FindBestCategoryMatch(existingCategories, new[] { "tinh bột", "carbohydrate", "grain" });

            if (ContainsAny(lowerName, new[] { "trứng", "egg", "đậu hũ", "tofu", "đậu phụ", "đậu nành", "soy" }))
                return FindBestCategoryMatch(existingCategories, new[] { "protein thực vật", "plant protein", "vegetarian protein" });

            if (ContainsAny(lowerName, new[] { "nấm", "mushroom", "nấm hương", "nấm đông cô", "nấm rơm" }))
                return FindBestCategoryMatch(existingCategories, new[] { "nấm", "mushroom" });

            if (ContainsAny(lowerName, new[] { "hành", "tỏi", "ginger", "gừng", "sả", "lemongrass", "hành lá", "hành tây" }))
                return FindBestCategoryMatch(existingCategories, new[] { "gia vị tươi", "fresh herbs", "aromatics" });

            // If no match found, create a new category
            return CreateNewCategoryName(ingredientName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding/creating ingredient category for: {IngredientName}", ingredientName);
            return "AI Generated";
        }
    }

    private static bool ContainsAny(string text, string[] patterns)
    {
        return patterns.Any(pattern => text.Contains(pattern));
    }

    private static string FindBestCategoryMatch(List<string> existingCategories, string[] preferredCategories)
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

    private static string CreateNewCategoryName(string ingredientName)
    {
        var lowerName = ingredientName.ToLower();
        
        // Create meaningful category names for Vietnamese ingredients
        if (ContainsAny(lowerName, new[] { "lá", "leaf", "rau thơm", "herb" }))
            return "Lá gia vị"; // Herb leaves
        
        if (ContainsAny(lowerName, new[] { "hạt", "seed", "nut", "đậu", "bean" }))
            return "Hạt & Đậu"; // Seeds & Nuts
        
        if (ContainsAny(lowerName, new[] { "nước", "liquid", "broth", "soup", "canh" }))
            return "Nước dùng"; // Liquids/Broths
        
        if (ContainsAny(lowerName, new[] { "bột", "flour", "starch", "tinh bột" }))
            return "Bột & Tinh bột"; // Flours & Starches
        
        if (ContainsAny(lowerName, new[] { "trái cây", "fruit", "quả" }))
            return "Trái cây"; // Fruits
        
        if (ContainsAny(lowerName, new[] { "sữa", "milk", "cream", "bơ", "butter" }))
            return "Sữa & Bơ sữa"; // Dairy & Dairy Products
        
        return "AI Generated"; // Default fallback
    }

    /// <summary>
    /// Validates and cleans image URLs to prevent database issues
    /// </summary>
    private string? ValidateAndCleanImageUrl(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return null;

        try
        {
            // Check URL length - most databases have limits, let's use 500 characters as a safe limit
            if (imageUrl.Length > 500)
            {
                _logger.LogWarning("Image URL too long ({Length} characters), truncating or rejecting", imageUrl.Length);
                
                // Try to find a reasonable truncation point
                var truncatedUrl = TruncateImageUrl(imageUrl);
                if (!string.IsNullOrEmpty(truncatedUrl))
                {
                    imageUrl = truncatedUrl;
                }
                else
                {
                    return null; // Reject if we can't truncate safely
                }
            }

            // Validate URL format
            if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri))
            {
                _logger.LogWarning("Invalid image URL format: {ImageUrl}", imageUrl);
                return null;
            }

            // Check if it's HTTP/HTTPS
            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            {
                _logger.LogWarning("Image URL must be HTTP/HTTPS: {ImageUrl}", imageUrl);
                return null;
            }

            // Check for common image file extensions or image hosting domains
            var validImageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp" };
            var validImageHosts = new[] { "images.unsplash.com", "cdn.pixabay.com", "images.pexels.com", 
                                        "www.simplyrecipes.com", "images.immediate.co.uk", "www.foodnetwork.com" };

            var hasValidExtension = validImageExtensions.Any(ext => 
                uri.AbsolutePath.ToLower().Contains(ext));
            var hasValidHost = validImageHosts.Any(host => 
                uri.Host.ToLower().Contains(host.ToLower()));

            // If it doesn't have a valid extension or host, it might still be valid (some URLs don't show extensions)
            // So we'll allow it but log a warning
            if (!hasValidExtension && !hasValidHost)
            {
                _logger.LogDebug("Image URL doesn't have recognizable image extension or host, but allowing: {ImageUrl}", imageUrl);
            }

            return imageUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating image URL: {ImageUrl}", imageUrl);
            return null;
        }
    }

    /// <summary>
    /// Attempts to truncate a long image URL at a reasonable point
    /// </summary>
    private string? TruncateImageUrl(string imageUrl)
    {
        try
        {
            // Look for common patterns where we can safely truncate
            var truncationPatterns = new[]
            {
                "-456", // Repeated pattern in your example
                "?", // Query parameters
                "#", // Fragment
                "/thumb", // Thumbnail indicators
                "/resize", // Resize parameters
            };

            foreach (var pattern in truncationPatterns)
            {
                var index = imageUrl.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
                if (index > 0 && index < 400) // Make sure we're not truncating too early
                {
                    var truncated = imageUrl.Substring(0, index);
                    
                    // Add common image extension if missing
                    if (!truncated.ToLower().EndsWith(".jpg") && 
                        !truncated.ToLower().EndsWith(".jpeg") && 
                        !truncated.ToLower().EndsWith(".png"))
                    {
                        truncated += ".jpg"; // Default to jpg
                    }
                    
                    _logger.LogInformation("Truncated long image URL from {OriginalLength} to {NewLength} characters", 
                        imageUrl.Length, truncated.Length);
                    
                    return truncated;
                }
            }

            // If no pattern found, try to truncate at a reasonable point
            if (imageUrl.Length > 400)
            {
                var truncated = imageUrl.Substring(0, 400);
                
                // Try to end at a reasonable character
                var lastSlash = truncated.LastIndexOf('/');
                var lastDot = truncated.LastIndexOf('.');
                
                if (lastDot > lastSlash && lastDot > 350)
                {
                    // Truncate after the file extension
                    var extension = truncated.Substring(lastDot);
                    if (extension.Length <= 5) // Reasonable extension length
                    {
                        return truncated.Substring(0, lastDot + Math.Min(4, extension.Length));
                    }
                }
                
                if (lastSlash > 300)
                {
                    return truncated.Substring(0, lastSlash) + ".jpg";
                }
            }

            return null; // Can't truncate safely
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error truncating image URL");
            return null;
        }
    }
} 