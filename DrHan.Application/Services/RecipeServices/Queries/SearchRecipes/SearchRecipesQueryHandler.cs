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

            // Step 2: Check if we need AI recipes (only on first page with no results)
            var shouldUseAI = searchDto.Page == 1 && dbRecipes.Items.Count == 0;

            // Step 3: If first page has no results, get exactly 5 from AI
            if (shouldUseAI)
            {
                var aiRecipes = await TryGetRecipesFromAI(searchDto, 5, cancellationToken); // Fixed count to 5
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
    /// Try to get recipes from AI - only when first page has no results
    /// </summary>
    private async Task<List<Recipe>> TryGetRecipesFromAI(
        RecipeSearchDto searchDto,
        int count,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("No recipes found in database for first page, requesting {Count} AI recipes", count);

            var geminiRequest = CreateGeminiRequest(searchDto, count);
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
            Servings = 1,
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
                var recipe = await CreateRecipeFromGeminiDataAsync(geminiRecipe, cancellationToken);
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
    private async Task<Recipe> CreateRecipeFromGeminiDataAsync(GeminiRecipeResponseDto geminiRecipe, CancellationToken cancellationToken)
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
        await AddIngredientsToRecipe(recipe, geminiRecipe.Ingredients, cancellationToken);
        AddInstructionsToRecipe(recipe, geminiRecipe.Instructions);
        AddAllergensToRecipe(recipe, geminiRecipe.Allergens);
        AddAllergenFreeClaimsToRecipe(recipe, geminiRecipe.AllergenFreeClaims);

        return recipe;
    }

    private async Task AddIngredientsToRecipe(Recipe recipe, List<GeminiIngredientDto> ingredients, CancellationToken cancellationToken)
    {
        if (ingredients == null || !ingredients.Any())
            return;

        // Batch process ingredients to avoid multiple database round trips
        var ingredientNames = ingredients.Select(i => i.Name.ToLower()).ToList();
        var existingIngredients = await _unitOfWork.Repository<Ingredient>()
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
                    // Create new ingredient if it doesn't exist
                    existingIngredient = new Ingredient
                    {
                        BusinessId = Guid.NewGuid(),
                        Name = ingredient.Name,
                        Description = $"Added automatically for recipe: {recipe.Name}",
                        CreateAt = DateTime.UtcNow,
                        UpdateAt = DateTime.UtcNow
                    };
                    newIngredients.Add(existingIngredient);
                    existingIngredientDict[lowerName] = existingIngredient; // Add to dict for future reference
                }

                recipe.RecipeIngredients.Add(new RecipeIngredient
                {
                    BusinessId = Guid.NewGuid(),
                    Recipe = recipe,
                    Ingredient = existingIngredient, // Use navigation property instead of ID
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
            await _unitOfWork.Repository<Ingredient>().AddRangeAsync(newIngredients);
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
}