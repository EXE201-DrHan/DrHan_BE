using MediatR;
using DrHan.Application.DTOs.Ingredients;
using DrHan.Application.Commons;
using DrHan.Application.Interfaces.Repository;
using DrHan.Domain.Entities.Ingredients;
using Microsoft.Extensions.Logging;
using DrHan.Application.Interfaces.Services.CacheService;

namespace DrHan.Application.Services.IngredientServices.Queries.GetIngredientCategories;

public class GetIngredientCategoriesQueryHandler : IRequestHandler<GetIngredientCategoriesQuery, AppResponse<List<IngredientCategoryDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetIngredientCategoriesQueryHandler> _logger;
    private readonly ICacheKeyService _cacheKeyService;
    private readonly ICacheService _cacheService;

    public GetIngredientCategoriesQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetIngredientCategoriesQueryHandler> logger,
        ICacheKeyService cacheKeyService,
        ICacheService cacheService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _cacheKeyService = cacheKeyService;
        _cacheService = cacheService;
    }

    public async Task<AppResponse<List<IngredientCategoryDto>>> Handle(GetIngredientCategoriesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var cacheKey = _cacheKeyService.Custom("ingredient", "categories");
            
            // Try to get categories from cache first
            var cachedCategories = await _cacheService.GetAsync<List<IngredientCategoryDto>>(cacheKey);
            if (cachedCategories != null)
            {
                _logger.LogInformation("Retrieved ingredient categories from cache");
                return new AppResponse<List<IngredientCategoryDto>>()
                    .SetSuccessResponse(cachedCategories);
            }

            // If not in cache, fetch from database
            var ingredients = await _unitOfWork.Repository<Ingredient>().ListAllAsync();
            
            var categories = ingredients
                .Where(i => !string.IsNullOrEmpty(i.Category))
                .GroupBy(i => i.Category)
                .Select(g => new IngredientCategoryDto { Category = g.Key!, Count = g.Count() })
                .OrderBy(c => c.Category)
                .ToList();

            // Cache the result for future requests
            await _cacheService.SetAsync(cacheKey, categories, TimeSpan.FromHours(24));
            _logger.LogInformation("Cached ingredient categories for 24 hours");

            return new AppResponse<List<IngredientCategoryDto>>().SetSuccessResponse(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ingredient categories");
            return new AppResponse<List<IngredientCategoryDto>>()
                .SetErrorResponse("GetIngredientCategories", "An error occurred while retrieving ingredient categories");
        }
    }
} 