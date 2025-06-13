using AutoMapper;
using DrHan.Application.Commons;
using DrHan.Application.DTOs.Gemini;
using DrHan.Application.DTOs.Recipes;
using DrHan.Application.Interfaces.Repository;
using DrHan.Application.Interfaces.Services;
using DrHan.Application.StaticQuery;
using DrHan.Domain.Entities.Ingredients;
using DrHan.Domain.Entities.Recipes;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace DrHan.Application.Services.RecipeServices.Queries.SearchRecipes;

public class SearchRecipesQueryHandler : IRequestHandler<SearchRecipesQuery, AppResponse<IPaginatedList<RecipeDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IGeminiRecipeService _geminiService;
    private readonly ILogger<SearchRecipesQueryHandler> _logger;

    public SearchRecipesQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IGeminiRecipeService geminiService,
        ILogger<SearchRecipesQueryHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _geminiService = geminiService ?? throw new ArgumentNullException(nameof(geminiService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AppResponse<IPaginatedList<RecipeDto>>> Handle(
        SearchRecipesQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var searchDto = request.SearchDto;

            // Step 1: Search existing recipes in database
            var dbRecipes = await GetRecipesFromDatabase(searchDto, cancellationToken);

            // Step 2: Check if we have enough results
            var hasEnoughResults = dbRecipes.Items.Count >= searchDto.PageSize;
            var isFirstPage = searchDto.Page == 1;

            // Step 3: If not enough results on first page, try getting more from AI
            if (!hasEnoughResults && isFirstPage)
            {
                var aiRecipes = await TryGetRecipesFromAI(searchDto, dbRecipes.Items.Count, cancellationToken);
                if (aiRecipes.Any())
                {
                    return CreateSuccessResponse(dbRecipes, aiRecipes, searchDto, "Results include AI-generated recipes");
                }
            }

            // Step 4: Return what we have from database
            return CreateSuccessResponse(dbRecipes, new List<Recipe>(), searchDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching recipes");
            return new AppResponse<IPaginatedList<RecipeDto>>()
                .SetErrorResponse("Error", "An error occurred while searching recipes");
        }
    }

    /// <summary>
    /// Get recipes from database using search criteria
    /// </summary>
    private async Task<IPaginatedList<Recipe>> GetRecipesFromDatabase(
        RecipeSearchDto searchDto,
        CancellationToken cancellationToken)
    {
        var paginationRequest = new PaginationRequest(searchDto.Page, searchDto.PageSize);

        return await _unitOfWork.Repository<Recipe>().ListAsyncWithPaginated(
            filter: RecipeSearchQuery.BuildFilter(searchDto),
            orderBy: RecipeSearchQuery.BuildOrderBy(searchDto),
            includeProperties: RecipeSearchQuery.BuildSearchIncludes(),
            pagination: paginationRequest,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Try to get additional recipes from AI if database doesn't have enough
    /// </summary>
    private async Task<List<Recipe>> TryGetRecipesFromAI(
        RecipeSearchDto searchDto,
        int existingCount,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Only found {Count} recipes in database, trying AI for more results", existingCount);

            var recipesNeeded = searchDto.PageSize - existingCount;
            var geminiRequest = CreateGeminiRequest(searchDto, recipesNeeded);
            var geminiRecipes = await _geminiService.SearchRecipesAsync(geminiRequest);

            if (geminiRecipes.Any())
            {
                return await ConvertAndSaveGeminiRecipes(geminiRecipes, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get recipes from AI");
        }

        return new List<Recipe>();
    }

    /// <summary>
    /// Create a success response with database and AI recipes combined
    /// </summary>
    private AppResponse<IPaginatedList<RecipeDto>> CreateSuccessResponse(
        IPaginatedList<Recipe> dbRecipes,
        List<Recipe> aiRecipes,
        RecipeSearchDto searchDto,
        string? message = null)
    {
        // Combine all recipes
        var allRecipes = dbRecipes.Items.ToList();
        allRecipes.AddRange(aiRecipes);

        // Convert to DTOs
        var recipeDtos = _mapper.Map<List<RecipeDto>>(allRecipes);

        // Create paginated result
        var result = PaginatedList<RecipeDto>.Create(
            recipeDtos,
            searchDto.Page,
            searchDto.PageSize,
            dbRecipes.TotalCount + aiRecipes.Count);

        return new AppResponse<IPaginatedList<RecipeDto>>()
            .SetSuccessResponse(result, "Success", message);
    }

    /// <summary>
    /// Create a request to ask AI for more recipes
    /// </summary>
    private GeminiRecipeRequestDto CreateGeminiRequest(RecipeSearchDto searchDto, int count)
    {
        return new GeminiRecipeRequestDto
        {
            SearchQuery = searchDto.SearchTerm ?? "popular recipes",
            CuisineType = searchDto.CuisineType,
            MealType = searchDto.MealType,
            Servings = 1, // No serving size requirement
            ExcludeAllergens = searchDto.ExcludeAllergens,
            Count = count
        };
    }

    /// <summary>
    /// Convert AI recipes to database entities and save them
    /// </summary>
    private async Task<List<Recipe>> ConvertAndSaveGeminiRecipes(
        List<GeminiRecipeResponseDto> geminiRecipes,
        CancellationToken cancellationToken)
    {
        var newRecipes = new List<Recipe>();

        foreach (var geminiRecipe in geminiRecipes)
        {
            try
            {
                // Skip if recipe already exists
                if (await RecipeAlreadyExists(geminiRecipe))
                    continue;

                // Convert AI recipe to database recipe
                var recipe = await CreateRecipeFromGeminiDataAsync(geminiRecipe);
                newRecipes.Add(recipe);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to convert recipe: {RecipeName}", geminiRecipe.Name);
            }
        }

        // Save all new recipes to database
        if (newRecipes.Any())
        {
            await _unitOfWork.Repository<Recipe>().AddRangeAsync(newRecipes);
            await _unitOfWork.CompleteAsync(cancellationToken);
            _logger.LogInformation("Saved {Count} new AI recipes to database", newRecipes.Count);
        }

        return newRecipes;
    }

    /// <summary>
    /// Check if a recipe with the same name and cuisine already exists
    /// </summary>
    private async Task<bool> RecipeAlreadyExists(GeminiRecipeResponseDto geminiRecipe)
    {
        return await _unitOfWork.Repository<Recipe>()
            .ExistsAsync(r => r.Name.ToLower() == geminiRecipe.Name.ToLower() &&
                            r.CuisineType.ToLower() == geminiRecipe.CuisineType.ToLower());
    }

    /// <summary>
    /// Create a complete Recipe entity from Gemini AI data
    /// </summary>
    private async Task<Recipe> CreateRecipeFromGeminiDataAsync(GeminiRecipeResponseDto geminiRecipe)
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
            SourceUrl = "Generated by AI",
            OriginalAuthor = "AI Generated",
            CreateAt = DateTime.UtcNow,
            UpdateAt = DateTime.UtcNow
        };

        // Add all the recipe details
        await AddIngredientsToRecipe(recipe, geminiRecipe.Ingredients);
        AddInstructionsToRecipe(recipe, geminiRecipe.Instructions);
        AddAllergensToRecipe(recipe, geminiRecipe.Allergens);
        AddAllergenFreeClaimsToRecipe(recipe, geminiRecipe.AllergenFreeClaims);

        return recipe;
    }

    private async Task AddIngredientsToRecipe(Recipe recipe, List<GeminiIngredientDto> ingredients)
    {
        if (ingredients == null || !ingredients.Any())
            return;

        foreach (var ingredient in ingredients)
        {
            try
            {
                // Try to find existing ingredient by name
                var existingIngredient = (await _unitOfWork.Repository<Ingredient>()
                    .ListAsync(i => i.Name.ToLower() == ingredient.Name.ToLower()))
                    .FirstOrDefault();

                if (existingIngredient == null)
                {
                    // Create new ingredient if it doesn't exist
                    existingIngredient = new Ingredient
                    {
                        BusinessId = Guid.NewGuid(),
                        Name = ingredient.Name,
                        Description = $"Added automatically for recipe: {recipe.Name}",
                        CreateAt = DateTime.UtcNow,
                        UpdateAt = DateTime.UtcNow
                    };
                    await _unitOfWork.Repository<Ingredient>().AddAsync(existingIngredient);
                    await _unitOfWork.CompleteAsync();
                }

                recipe.RecipeIngredients.Add(new RecipeIngredient
                {
                    BusinessId = Guid.NewGuid(),
                    Recipe = recipe,
                    IngredientId = existingIngredient.Id,
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
    }

    private void AddInstructionsToRecipe(Recipe recipe, List<GeminiInstructionDto> instructions)
    {
        if (instructions == null || !instructions.Any())
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
                CreateAt = DateTime.UtcNow,
                UpdateAt = DateTime.UtcNow
            });
        }
    }

    private void AddAllergensToRecipe(Recipe recipe, List<string> allergens)
    {
        if (allergens == null || !allergens.Any())
            return;

        foreach (var allergen in allergens)
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
    }

    private void AddAllergenFreeClaimsToRecipe(Recipe recipe, List<string> claims)
    {
        if (claims == null || !claims.Any())
            return;

        foreach (var claim in claims)
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