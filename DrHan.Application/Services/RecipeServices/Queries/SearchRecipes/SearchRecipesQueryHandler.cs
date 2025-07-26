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
using Microsoft.Extensions.Caching.Memory; // ADD THIS
using System.Linq;

namespace DrHan.Application.Services.RecipeServices.Queries.SearchRecipes;

public class SearchRecipesQueryHandler : IRequestHandler<SearchRecipesQuery, AppResponse<IPaginatedList<RecipeDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IGeminiRecipeService _geminiService;
    private readonly IRecipePersistenceService _recipePersistenceService;
    private readonly ILogger<SearchRecipesQueryHandler> _logger;
    private readonly IMemoryCache _memoryCache; // ADD THIS

    // ADD THESE OPTIMIZATION CONSTANTS
    private const int QUERY_CACHE_MINUTES = 2; // Cache identical queries for 2 minutes
    private const int AI_RECIPE_CACHE_MINUTES = 30; // Cache AI recipe lookups

    public SearchRecipesQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IGeminiRecipeService geminiService,
        IRecipePersistenceService recipePersistenceService,
        IMemoryCache memoryCache, // ADD THIS
        ILogger<SearchRecipesQueryHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _geminiService = geminiService ?? throw new ArgumentNullException(nameof(geminiService));
        _recipePersistenceService = recipePersistenceService ?? throw new ArgumentNullException(nameof(recipePersistenceService));
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache)); // ADD THIS
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AppResponse<IPaginatedList<RecipeDto>>> Handle(
        SearchRecipesQuery request,
        CancellationToken cancellationToken)
    {
        var searchDto = request.SearchDto;
        
        _logger.LogInformation("🔍 Starting search: '{SearchTerm}' | Page: {Page} | Size: {PageSize}", 
            searchDto.SearchTerm, searchDto.Page, searchDto.PageSize);

        try
        {
            var dbRecipes = await GetRecipesFromDatabaseAsync(searchDto, cancellationToken);
            
            _logger.LogInformation("📊 Found {Count} recipes in database for '{SearchTerm}' page {Page}", 
                dbRecipes.Items.Count, searchDto.SearchTerm, searchDto.Page);

            var needsMoreRecipes = dbRecipes.Items.Count < 10;

            if (needsMoreRecipes)
            {
                _logger.LogInformation("📈 Need more recipes ({Count} < 10), checking AI for '{SearchTerm}'", 
                    dbRecipes.Items.Count, searchDto.SearchTerm);
                
                // OPTIMIZED: Check cached AI recipes first
                var existingAIRecipes = await GetCachedAIRecipesAsync(searchDto, cancellationToken);
                
                if (existingAIRecipes.Any())
                {
                    _logger.LogInformation("⚡ Using {Count} cached AI recipes for '{SearchTerm}'", 
                        existingAIRecipes.Count, searchDto.SearchTerm);
                    
                    return CreateSuccessResponseWithExistingAI(dbRecipes, existingAIRecipes, searchDto);
                }
                else
                {
                    // Generate new AI recipes and show them immediately, then persist later
                    _logger.LogInformation("🤖 Generating new AI recipes for '{SearchTerm}'", searchDto.SearchTerm);
                    var aiRecipeDtos = await TryGetRecipesFromAIAsync(searchDto, dbRecipes.Items.Count, cancellationToken);
                    
                    if (aiRecipeDtos.Any())
                    {
                        _logger.LogInformation("✅ Generated {Count} AI recipes for '{SearchTerm}', showing immediately", 
                            aiRecipeDtos.Count, searchDto.SearchTerm);
                        
                        // Queue recipes for background persistence (fire and forget)
                        var searchContext = $"{searchDto.SearchTerm}|{searchDto.CuisineType}|{searchDto.MealType}|Page:{searchDto.Page}";
                        _ = Task.Run(async () => 
                        {
                            try
                            {
                                await _recipePersistenceService.QueueRecipesForPersistenceAsync(aiRecipeDtos, searchContext);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failed to queue AI recipes for persistence");
                            }
                        }, cancellationToken);
                        
                        // Return results immediately without waiting for database persistence
                        return CreateSuccessResponseWithAIDtos(dbRecipes, aiRecipeDtos, searchDto, "Kết quả bao gồm công thức mới do AI tạo");
                    }
                    else
                    {
                        _logger.LogWarning("❌ Could not generate AI recipes for '{SearchTerm}'", searchDto.SearchTerm);
                    }
                }
            }

            // Return what we have from database
            _logger.LogInformation("📋 Returning {Count} database recipes for '{SearchTerm}'", 
                dbRecipes.Items.Count, searchDto.SearchTerm);
            return CreateSuccessResponse(dbRecipes, searchDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Search failed for '{SearchTerm}', Page: {Page}", 
                searchDto.SearchTerm, searchDto.Page);
            return new AppResponse<IPaginatedList<RecipeDto>>()
                .SetErrorResponse("Lỗi", "Đã xảy ra lỗi khi tìm kiếm công thức nấu ăn");
        }
    }

    /// <summary>
    /// OPTIMIZED: Get recipes from database with caching for repeated queries
    /// </summary>
    private async Task<IPaginatedList<Recipe>> GetRecipesFromDatabaseAsync(
        RecipeSearchDto searchDto,
        CancellationToken cancellationToken)
    {
        // OPTIMIZATION 1: Cache identical queries for 2 minutes
        var cacheKey = $"db_search_{searchDto.GetHashCode()}";
        if (_memoryCache.TryGetValue(cacheKey, out IPaginatedList<Recipe>? cachedResult))
        {
            _logger.LogDebug("⚡ Database query cache hit for '{SearchTerm}'", searchDto.SearchTerm);
            return cachedResult!;
        }

        var paginationRequest = new PaginationRequest(searchDto.Page, searchDto.PageSize);

        //var result = await _unitOfWork.Repository<Recipe>().ListAsyncWithPaginated(
        //    filter: RecipeSearchQuery.BuildFilter(searchDto),
        //    orderBy: RecipeSearchQuery.BuildOrderBy(searchDto),
        //    includeProperties: RecipeSearchQuery.BuildSearchIncludes(),
        //    pagination: paginationRequest,
        //    cancellationToken: cancellationToken);

        var result = await _unitOfWork.Repository<Recipe>().ListAsyncWithPaginated(
            filter: c => c.Name.Contains(searchDto.SearchTerm),
            orderBy: RecipeSearchQuery.BuildOrderBy(searchDto),
            includeProperties: RecipeSearchQuery.BuildSearchIncludes(),
            pagination: paginationRequest,
            cancellationToken: cancellationToken);
        // Cache for 2 minutes to handle rapid repeated requests
        _memoryCache.Set(cacheKey, result, TimeSpan.FromMinutes(QUERY_CACHE_MINUTES));

        return result;
    }

    /// <summary>
    /// OPTIMIZED: Get AI recipes with caching to avoid repeated database queries
    /// </summary>
    private async Task<List<Recipe>> GetCachedAIRecipesAsync(RecipeSearchDto searchDto, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(searchDto.SearchTerm))
            return new List<Recipe>();

        // OPTIMIZATION 2: Cache AI recipe lookups for 30 minutes
        var cacheKey = $"ai_recipes_{searchDto.SearchTerm?.ToLower()}_{searchDto.CuisineType}_{searchDto.MealType}";
        if (_memoryCache.TryGetValue(cacheKey, out List<Recipe>? cachedAIRecipes))
        {
            _logger.LogDebug("⚡ AI recipes cache hit for '{SearchTerm}'", searchDto.SearchTerm);
            return cachedAIRecipes!;
        }

        // OPTIMIZATION 3: Use more efficient query with EF.Functions.Contains for better performance
        var aiRecipes = await _unitOfWork.Repository<Recipe>()
            .ListAsync(r => r.OriginalAuthor == "AI Generated" &&
                           (EF.Functions.Like(r.Name, searchDto.SearchTerm) ||
                            EF.Functions.Like(r.Description, searchDto.SearchTerm) ||
                            r.RecipeIngredients.Any(ri => EF.Functions.Like(ri.IngredientName, searchDto.SearchTerm))));

        var result = aiRecipes.Take(5).ToList(); // Return max 5 existing AI recipes

        // Cache the results for 30 minutes
        _memoryCache.Set(cacheKey, result, TimeSpan.FromMinutes(AI_RECIPE_CACHE_MINUTES));

        if (result.Any())
        {
            _logger.LogInformation("🎯 Found {Count} existing AI recipes for '{SearchTerm}'", result.Count, searchDto.SearchTerm);
        }

        return result;
    }

    /// <summary>
    /// Get recipes from AI and return as DTOs immediately (without persisting to database)
    /// </summary>
    private async Task<List<GeminiRecipeResponseDto>> TryGetRecipesFromAIAsync(
        RecipeSearchDto searchDto,
        int count,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Yêu cầu {Count} công thức từ AI để bổ sung kết quả", count);

            var geminiRequest = CreateGeminiRequest(searchDto, count);
            var geminiRecipes = await _geminiService.SearchRecipesAsync(geminiRequest);

            if (geminiRecipes.Any())
            {
                _logger.LogInformation("Nhận được {Count} công thức từ Gemini API", geminiRecipes.Count);
                return geminiRecipes;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Không thể lấy công thức từ AI");
        }

        return new List<GeminiRecipeResponseDto>();
    }

    /// <summary>
    /// Create a success response with only database recipes
    /// </summary>
    private AppResponse<IPaginatedList<RecipeDto>> CreateSuccessResponse(
        IPaginatedList<Recipe> dbRecipes,
        RecipeSearchDto searchDto,
        string? message = null)
    {
        // Convert to DTOs
        var recipeDtos = _mapper.Map<List<RecipeDto>>(dbRecipes.Items);

        // Create paginated result
        var result = PaginatedList<RecipeDto>.Create(
            recipeDtos,
            searchDto.Page,
            searchDto.PageSize,
            dbRecipes.TotalCount);

        return new AppResponse<IPaginatedList<RecipeDto>>()
            .SetSuccessResponse(result, "Thành công", message);
    }

    /// <summary>
    /// Create a success response with database and existing AI recipes combined
    /// </summary>
    private AppResponse<IPaginatedList<RecipeDto>> CreateSuccessResponseWithExistingAI(
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
            .SetSuccessResponse(result, "Thành công", message ?? "Kết quả bao gồm công thức do AI tạo");
    }

    /// <summary>
    /// Create a success response with database recipes and new AI recipe DTOs
    /// </summary>
    private AppResponse<IPaginatedList<RecipeDto>> CreateSuccessResponseWithAIDtos(
        IPaginatedList<Recipe> dbRecipes,
        List<GeminiRecipeResponseDto> aiRecipeDtos,
        RecipeSearchDto searchDto,
        string? message = null)
    {
        // Convert database recipes to DTOs
        var dbRecipeDtos = _mapper.Map<List<RecipeDto>>(dbRecipes.Items);

        // Convert AI recipes to DTOs (without database IDs)
        var aiDtos = aiRecipeDtos.Select(ai => new RecipeDto
        {
            // Use negative IDs for AI recipes to distinguish them (temporary until persisted)
            Id = 0, 
            BusinessId = Guid.NewGuid(),
            Name = ai.Name,
            Description = ai.Description,
            CuisineType = ai.CuisineType,
            MealType = ai.MealType,
            PrepTimeMinutes = ai.PrepTimeMinutes,
            CookTimeMinutes = ai.CookTimeMinutes,
            Servings = ai.Servings,
            DifficultyLevel = ai.DifficultyLevel,
            IsCustom = false,
            IsPublic = true,
            ThumbnailImageUrl = "Được tạo bởi AI",
            CreateAt = DateTime.Now,
            UpdateAt = DateTime.Now
        }).ToList();

        // Combine all recipe DTOs
        var allRecipeDtos = dbRecipeDtos.Concat(aiDtos).ToList();

        // Create paginated result
        var result = PaginatedList<RecipeDto>.Create(
            allRecipeDtos,
            searchDto.Page,
            searchDto.PageSize,
            dbRecipes.TotalCount + aiRecipeDtos.Count);

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
            CreateAt = DateTime.Now,
            UpdateAt = DateTime.Now
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
                        CreateAt = DateTime.Now,
                        UpdateAt = DateTime.Now
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
                        CreateAt = DateTime.Now,
                        UpdateAt = DateTime.Now
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
                        CreateAt = DateTime.Now,
                        UpdateAt = DateTime.Now
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
                            CreateAt = DateTime.Now,
                            UpdateAt = DateTime.Now
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
                                    CreateAt = DateTime.Now,
                                    UpdateAt = DateTime.Now
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
                CreateAt = DateTime.Now,
                UpdateAt = DateTime.Now
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
                CreateAt = DateTime.Now,
                UpdateAt = DateTime.Now
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
                CreateAt = DateTime.Now,
                UpdateAt = DateTime.Now
            });
        }
    }
}