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
        var searchDto = request.SearchDto;
        
        _logger.LogInformation("Bắt đầu tìm kiếm công thức với từ khóa: {SearchTerm}, Trang: {Page}, Kích thước trang: {PageSize}", 
            searchDto.SearchTerm, searchDto.Page, searchDto.PageSize);

        try
        {
            var dbRecipes = await GetRecipesFromDatabase(searchDto, cancellationToken);
            
            _logger.LogInformation("Tìm thấy {Count} công thức trong database cho từ khóa '{SearchTerm}'", 
                dbRecipes.Items.Count, searchDto.SearchTerm);

            var shouldUseAI = searchDto.Page == 1 && dbRecipes.Items.Count == 0;

            if (shouldUseAI)
            {
                _logger.LogInformation("Không tìm thấy công thức nào trong database, kiểm tra công thức AI hiện có");
                
                // Check if we already have AI-generated recipes for this search term
                var existingAIRecipes = await CheckForExistingAIRecipes(searchDto, cancellationToken);
                
                if (existingAIRecipes.Any())
                {
                    _logger.LogInformation("Sử dụng {Count} công thức AI hiện có cho từ khóa '{SearchTerm}'", 
                        existingAIRecipes.Count, searchDto.SearchTerm);
                    
                    // We already have AI recipes for this search, return them instead of generating new ones
                    return CreateSuccessResponse(dbRecipes, existingAIRecipes, searchDto, "Kết quả bao gồm công thức do AI tạo");
                }
                else
                {
                    // No existing AI recipes, generate new ones
                    _logger.LogInformation("Không tìm thấy công thức AI cho từ khóa '{SearchTerm}', tạo mới", searchDto.SearchTerm);
                    var aiRecipes = await TryGetRecipesFromAI(searchDto, 3, cancellationToken); // Reduced to 3 for complete responses
                    if (aiRecipes.Any())
                    {
                        _logger.LogInformation("Đã tạo thành công {Count} công thức AI mới cho từ khóa '{SearchTerm}'", 
                            aiRecipes.Count, searchDto.SearchTerm);
                        return CreateSuccessResponse(dbRecipes, aiRecipes, searchDto, "Kết quả bao gồm công thức do AI tạo");
                    }
                    else
                    {
                        _logger.LogWarning("Không thể tạo công thức AI cho từ khóa '{SearchTerm}'", searchDto.SearchTerm);
                    }
                }
            }

            // Step 4: Return what we have from database
            _logger.LogInformation("Trả về {Count} công thức từ database cho từ khóa '{SearchTerm}'", 
                dbRecipes.Items.Count, searchDto.SearchTerm);
            return CreateSuccessResponse(dbRecipes, new List<Recipe>(), searchDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tìm kiếm công thức với từ khóa '{SearchTerm}', Trang: {Page}", 
                searchDto.SearchTerm, searchDto.Page);
            return new AppResponse<IPaginatedList<RecipeDto>>()
                .SetErrorResponse("Lỗi", "Đã xảy ra lỗi khi tìm kiếm công thức nấu ăn");
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
            _logger.LogInformation("Không tìm thấy công thức trong cơ sở dữ liệu cho trang đầu, yêu cầu {Count} công thức từ AI", count);

            var geminiRequest = CreateGeminiRequest(searchDto, count);
            var geminiRecipes = await _geminiService.SearchRecipesAsync(geminiRequest);

            if (geminiRecipes.Any())
            {
                return await ConvertAndSaveGeminiRecipes(geminiRecipes, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Không thể lấy công thức từ AI");
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
            .SetSuccessResponse(result, "Thành công", message);
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
    /// Check for existing AI-generated recipes that match the search criteria
    /// </summary>
    private async Task<List<Recipe>> CheckForExistingAIRecipes(RecipeSearchDto searchDto, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(searchDto.SearchTerm))
            return new List<Recipe>();

        // Look for AI-generated recipes that contain the search term in name or description
        // We'll always return existing AI recipes if they exist, regardless of age
        // This prevents duplicate recipe generation
        var aiRecipes = await _unitOfWork.Repository<Recipe>()
            .ListAsync(r => r.OriginalAuthor == "AI Generated" &&
                           (r.Name.ToLower().Contains(searchDto.SearchTerm.ToLower()) ||
                            r.Description.ToLower().Contains(searchDto.SearchTerm.ToLower()) ||
                            r.RecipeIngredients.Any(ri => ri.IngredientName.ToLower().Contains(searchDto.SearchTerm.ToLower()))));

        if (aiRecipes.Any())
        {
            _logger.LogInformation("Tìm thấy {Count} công thức AI hiện có cho từ khóa '{SearchTerm}'", aiRecipes.Count, searchDto.SearchTerm);
            return aiRecipes.Take(5).ToList(); // Return max 5 existing AI recipes
        }

        return new List<Recipe>();
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
            SourceUrl = "Được tạo bởi AI",
            OriginalAuthor = "AI Generated", // Keep this in English for filtering purposes
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

        // Save new ingredients immediately to avoid duplicate key violations
        if (newIngredients.Any())
        {
            await _unitOfWork.Repository<Ingredient>().AddRangeAsync(newIngredients);
            try
            {
                await _unitOfWork.CompleteAsync(cancellationToken);
                _logger.LogInformation("Saved {Count} new ingredients", newIngredients.Count);
            }
            catch (Exception ex)
            {
                // If we get a duplicate key error, it means another process inserted the same ingredient
                // In this case, we need to refresh our existing ingredients and retry
                if (ex.Message.Contains("duplicate key") || ex.Message.Contains("IX_Ingredients_Name"))
                {
                    _logger.LogWarning("Duplicate ingredient detected, refreshing and retrying");
                    await HandleDuplicateIngredientConflict(recipe, ingredients, cancellationToken);
                    return;
                }
                throw;
            }
        }
    }

    private async Task HandleDuplicateIngredientConflict(Recipe recipe, List<GeminiIngredientDto> ingredients, CancellationToken cancellationToken)
    {
        // Clear any pending changes that might have caused the conflict
        _unitOfWork.DetachAllEntities();
        
        // Clear existing recipe ingredients to start fresh
        recipe.RecipeIngredients.Clear();
        
        // Re-fetch all ingredients from database to get the latest state
        var ingredientNames = ingredients.Select(i => i.Name.ToLower()).ToList();
        var existingIngredients = await _unitOfWork.Repository<Ingredient>()
            .ListAsync(i => ingredientNames.Contains(i.Name.ToLower()));
        
        var existingIngredientDict = existingIngredients.ToDictionary(i => i.Name.ToLower(), i => i);
        
        foreach (var ingredient in ingredients)
        {
            try
            {
                var lowerName = ingredient.Name.ToLower();
                
                // If ingredient exists in database, use it
                if (existingIngredientDict.TryGetValue(lowerName, out var existingIngredient))
                {
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
                else
                {
                    // Ingredient still doesn't exist, create it individually with error handling
                    var newIngredient = new Ingredient
                    {
                        BusinessId = Guid.NewGuid(),
                        Name = ingredient.Name,
                        Description = $"Added automatically for recipe: {recipe.Name}",
                        CreateAt = DateTime.UtcNow,
                        UpdateAt = DateTime.UtcNow
                    };
                    
                    try
                    {
                        await _unitOfWork.Repository<Ingredient>().AddAsync(newIngredient);
                        await _unitOfWork.CompleteAsync(cancellationToken);
                        
                        recipe.RecipeIngredients.Add(new RecipeIngredient
                        {
                            BusinessId = Guid.NewGuid(),
                            Recipe = recipe,
                            Ingredient = newIngredient,
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
                        if (ex.Message.Contains("duplicate key") || ex.Message.Contains("IX_Ingredients_Name"))
                        {
                            _logger.LogWarning("Ingredient {IngredientName} was created by another process, fetching from database", ingredient.Name);
                            
                            // Another process created this ingredient, fetch it from database
                            var fetchedIngredient = await _unitOfWork.Repository<Ingredient>()
                                .FindAsync(i => i.Name.ToLower() == lowerName);
                            
                            if (fetchedIngredient != null)
                            {
                                recipe.RecipeIngredients.Add(new RecipeIngredient
                                {
                                    BusinessId = Guid.NewGuid(),
                                    Recipe = recipe,
                                    Ingredient = fetchedIngredient,
                                    IngredientName = ingredient.Name,
                                    Quantity = ingredient.Quantity,
                                    Unit = ingredient.Unit,
                                    PreparationNotes = ingredient.Notes,
                                    OrderInRecipe = recipe.RecipeIngredients.Count + 1,
                                    CreateAt = DateTime.UtcNow,
                                    UpdateAt = DateTime.UtcNow
                                });
                            }
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
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
}