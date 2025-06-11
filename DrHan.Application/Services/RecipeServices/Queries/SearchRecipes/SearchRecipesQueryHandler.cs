using AutoMapper;
using DrHan.Application.Commons;
using DrHan.Application.DTOs.Gemini;
using DrHan.Application.DTOs.Recipes;
using DrHan.Application.Interfaces.Repository;
using DrHan.Application.Interfaces.Services;
using DrHan.Domain.Entities.Recipes;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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
            
            // First, search in the database
            var paginationRequest = new PaginationRequest(searchDto.Page, searchDto.PageSize);
            var dbRecipes = await SearchInDatabaseAsync(searchDto, paginationRequest, cancellationToken);

            // If enough results from database or not the first page, return database results
            if (dbRecipes.Items.Count >= searchDto.PageSize || searchDto.Page > 1)
            {
                var dbRecipeDtos = _mapper.Map<List<RecipeDto>>(dbRecipes.Items);
                var dbPaginatedResult = PaginatedList<RecipeDto>.Create(
                    dbRecipeDtos,
                    dbRecipes.PageNumber,
                    dbRecipes.PageSize,
                    dbRecipes.TotalCount);

                return new AppResponse<IPaginatedList<RecipeDto>>()
                    .SetSuccessResponse(dbPaginatedResult);
            }

            // If insufficient results and first page, fetch from Gemini
            if (dbRecipes.Items.Count < searchDto.PageSize && searchDto.Page == 1)
            {
                try
                {
                    _logger.LogInformation("Insufficient database results ({Count}), fetching from Gemini API", 
                        dbRecipes.Items.Count);

                    var geminiRequest = CreateGeminiRequest(searchDto, searchDto.PageSize - dbRecipes.Items.Count);
                    var geminiRecipes = await _geminiService.SearchRecipesAsync(geminiRequest);

                    if (geminiRecipes.Any())
                    {
                        var newRecipes = await ConvertAndSaveGeminiRecipes(geminiRecipes, cancellationToken);
                        
                        // Combine results
                        var allRecipes = dbRecipes.Items.ToList();
                        allRecipes.AddRange(newRecipes);

                        var allRecipeDtos = _mapper.Map<List<RecipeDto>>(allRecipes);
                        var combinedResult = PaginatedList<RecipeDto>.Create(
                            allRecipeDtos,
                            searchDto.Page,
                            searchDto.PageSize,
                            dbRecipes.TotalCount + newRecipes.Count);

                        return new AppResponse<IPaginatedList<RecipeDto>>()
                            .SetSuccessResponse(combinedResult, "Success", "Results include recipes from AI generation");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching recipes from Gemini API");
                    // Continue with database results only
                }
            }

            // Return database results only
            var recipeDtos = _mapper.Map<List<RecipeDto>>(dbRecipes.Items);
            var finalResult = PaginatedList<RecipeDto>.Create(
                recipeDtos,
                dbRecipes.PageNumber,
                dbRecipes.PageSize,
                dbRecipes.TotalCount);

            return new AppResponse<IPaginatedList<RecipeDto>>()
                .SetSuccessResponse(finalResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SearchRecipesQueryHandler");
            return new AppResponse<IPaginatedList<RecipeDto>>()
                .SetErrorResponse("Error", "An error occurred while searching recipes");
        }
    }

    private async Task<IPaginatedList<Recipe>> SearchInDatabaseAsync(
        RecipeSearchDto searchDto, 
        PaginationRequest paginationRequest,
        CancellationToken cancellationToken)
    {
        return await _unitOfWork.Repository<Recipe>().ListAsyncWithPaginated(
            filter: BuildDatabaseFilter(searchDto),
            orderBy: BuildOrderBy(searchDto),
            includeProperties: BuildIncludes(),
            pagination: paginationRequest,
            cancellationToken: cancellationToken);
    }

    private System.Linq.Expressions.Expression<Func<Recipe, bool>>? BuildDatabaseFilter(RecipeSearchDto searchDto)
    {
        return recipe =>
            (string.IsNullOrEmpty(searchDto.SearchTerm) ||
             recipe.Name.Contains(searchDto.SearchTerm) ||
             recipe.Description.Contains(searchDto.SearchTerm) ||
             recipe.RecipeIngredients.Any(ri => ri.IngredientName.Contains(searchDto.SearchTerm))) &&

            (string.IsNullOrEmpty(searchDto.CuisineType) ||
             recipe.CuisineType == searchDto.CuisineType) &&

            (string.IsNullOrEmpty(searchDto.MealType) ||
             recipe.MealType == searchDto.MealType) &&

            (string.IsNullOrEmpty(searchDto.DifficultyLevel) ||
             recipe.DifficultyLevel == searchDto.DifficultyLevel) &&

            (!searchDto.MaxPrepTime.HasValue ||
             recipe.PrepTimeMinutes <= searchDto.MaxPrepTime) &&

            (!searchDto.MaxCookTime.HasValue ||
             recipe.CookTimeMinutes <= searchDto.MaxCookTime) &&

            (!searchDto.MinServings.HasValue ||
             recipe.Servings >= searchDto.MinServings) &&

            (!searchDto.MaxServings.HasValue ||
             recipe.Servings <= searchDto.MaxServings) &&

            (!searchDto.IsCustom.HasValue ||
             recipe.IsCustom == searchDto.IsCustom) &&

            (!searchDto.IsPublic.HasValue ||
             recipe.IsPublic == searchDto.IsPublic) &&

            (!searchDto.MinRating.HasValue ||
             recipe.RatingAverage >= searchDto.MinRating) &&

            (searchDto.ExcludeAllergens == null || !searchDto.ExcludeAllergens.Any() ||
             !recipe.RecipeAllergens.Any(ra => searchDto.ExcludeAllergens.Contains(ra.AllergenType))) &&

            (searchDto.RequireAllergenFree == null || !searchDto.RequireAllergenFree.Any() ||
             searchDto.RequireAllergenFree.All(claim =>
                 recipe.RecipeAllergenFreeClaims.Any(rc => rc.Claim == claim)));
    }

    private Func<IQueryable<Recipe>, IOrderedQueryable<Recipe>>? BuildOrderBy(RecipeSearchDto searchDto)
    {
        return searchDto.SortBy?.ToLower() switch
        {
            "rating" => searchDto.IsDescending 
                ? query => query.OrderByDescending(r => r.RatingAverage)
                : query => query.OrderBy(r => r.RatingAverage),
            "preptime" => searchDto.IsDescending
                ? query => query.OrderByDescending(r => r.PrepTimeMinutes)
                : query => query.OrderBy(r => r.PrepTimeMinutes),
            "cooktime" => searchDto.IsDescending
                ? query => query.OrderByDescending(r => r.CookTimeMinutes)
                : query => query.OrderBy(r => r.CookTimeMinutes),
            "likes" => searchDto.IsDescending
                ? query => query.OrderByDescending(r => r.LikesCount)
                : query => query.OrderBy(r => r.LikesCount),
            _ => searchDto.IsDescending
                ? query => query.OrderByDescending(r => r.Name)
                : query => query.OrderBy(r => r.Name)
        };
    }

    private Func<IQueryable<Recipe>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Recipe, object>>? BuildIncludes()
    {
        return query => query
            .Include(r => r.RecipeIngredients)
            .Include(r => r.RecipeInstructions)
            .Include(r => r.RecipeNutritions)
            .Include(r => r.RecipeAllergens)
            .Include(r => r.RecipeAllergenFreeClaims)
            .Include(r => r.RecipeImages);
    }

    private GeminiRecipeRequestDto CreateGeminiRequest(RecipeSearchDto searchDto, int count)
    {
        return new GeminiRecipeRequestDto
        {
            SearchQuery = searchDto.SearchTerm ?? "popular recipes",
            CuisineType = searchDto.CuisineType,
            MealType = searchDto.MealType,
            DifficultyLevel = searchDto.DifficultyLevel,
            MaxPrepTime = searchDto.MaxPrepTime,
            MaxCookTime = searchDto.MaxCookTime,
            Servings = searchDto.MinServings,
            ExcludeAllergens = searchDto.ExcludeAllergens,
            Count = count
        };
    }

    private async Task<List<Recipe>> ConvertAndSaveGeminiRecipes(
        List<GeminiRecipeResponseDto> geminiRecipes, 
        CancellationToken cancellationToken)
    {
        var newRecipes = new List<Recipe>();

        foreach (var geminiRecipe in geminiRecipes)
        {
            try
            {
                // Check if recipe already exists
                var exists = await _unitOfWork.Repository<Recipe>()
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

                newRecipes.Add(recipe);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting Gemini recipe: {RecipeName}", geminiRecipe.Name);
            }
        }

        if (newRecipes.Any())
        {
            await _unitOfWork.Repository<Recipe>().AddRangeAsync(newRecipes);
            await _unitOfWork.CompleteAsync();
            _logger.LogInformation("Successfully added {Count} new recipes from Gemini API", newRecipes.Count);
        }

        return newRecipes;
    }
} 