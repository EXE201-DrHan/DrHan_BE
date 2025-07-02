using MediatR;
using DrHan.Application.Commons;
using DrHan.Application.DTOs.Recipes;
using DrHan.Application.Interfaces.Repository;
using DrHan.Domain.Entities.Ingredients;
using DrHan.Domain.Entities.Recipes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DrHan.Application.Services.RecipeServices.Queries.GetRecipeFilterOptions;

public class GetRecipeFilterOptionsQueryHandler : IRequestHandler<GetRecipeFilterOptionsQuery, AppResponse<RecipeFilterOptionsDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetRecipeFilterOptionsQueryHandler> _logger;

    public GetRecipeFilterOptionsQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetRecipeFilterOptionsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<AppResponse<RecipeFilterOptionsDto>> Handle(GetRecipeFilterOptionsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Get dynamic filter options from database
            var recipes = await _unitOfWork.Repository<Recipe>().ListAsync(
                includeProperties: query => query
                    .Include(r => r.RecipeAllergens)
                    .Include(r => r.RecipeAllergenFreeClaims)
                    .Include(r => r.RecipeIngredients)
                        .ThenInclude(ri => ri.Ingredient)
            );

            // Get all ingredients from the database for comprehensive filtering
            var allIngredients = await _unitOfWork.Repository<Ingredient>().ListAsync();
            
            var filterOptions = new RecipeFilterOptionsDto
            {
                CuisineTypes = recipes
                    .Where(r => !string.IsNullOrEmpty(r.CuisineType))
                    .Select(r => r.CuisineType)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList(),
                    
                MealTypes = recipes
                    .Where(r => !string.IsNullOrEmpty(r.MealType))
                    .Select(r => r.MealType)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList(),
                    
                DifficultyLevels = recipes
                    .Where(r => !string.IsNullOrEmpty(r.DifficultyLevel))
                    .Select(r => r.DifficultyLevel)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList(),
                    
                SortOptions = new List<string>
                {
                    "Name", "Rating", "PrepTime", "Likes"
                },
                
                // Add available allergens from recipes
                AvailableAllergens = recipes
                    .SelectMany(r => r.RecipeAllergens)
                    .Where(ra => !string.IsNullOrEmpty(ra.AllergenType))
                    .Select(ra => ra.AllergenType)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList(),
                    
                // Add available allergen-free claims
                AvailableAllergenFreeClaims = recipes
                    .SelectMany(r => r.RecipeAllergenFreeClaims)
                    .Where(rc => !string.IsNullOrEmpty(rc.Claim))
                    .Select(rc => rc.Claim)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList(),

                // Add available ingredients from recipes
                AvailableIngredients = recipes
                    .SelectMany(r => r.RecipeIngredients)
                    .Where(ri => !string.IsNullOrEmpty(ri.IngredientName))
                    .Select(ri => ri.IngredientName)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList(),

                // Add ingredient categories
                IngredientCategories = allIngredients
                    .Where(i => !string.IsNullOrEmpty(i.Category))
                    .Select(i => i.Category!)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList()
            };

            _logger.LogInformation("Successfully retrieved recipe filter options");

            return new AppResponse<RecipeFilterOptionsDto>()
                .SetSuccessResponse(filterOptions, "Success", "Filter options retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recipe filter options");
            return new AppResponse<RecipeFilterOptionsDto>()
                .SetErrorResponse("Error", "An error occurred while retrieving filter options");
        }
    }
} 