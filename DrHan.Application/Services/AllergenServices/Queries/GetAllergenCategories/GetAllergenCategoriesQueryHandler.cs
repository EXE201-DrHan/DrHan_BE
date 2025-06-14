using MediatR;
using DrHan.Application.Commons;
using DrHan.Application.Interfaces.Repository;
using DrHan.Domain.Entities.Allergens;
using Microsoft.Extensions.Logging;
using DrHan.Application.Interfaces.Services.CacheService;

namespace DrHan.Application.Services.AllergenServices.Queries.GetAllergenCategories;

public class GetAllergenCategoriesQueryHandler : IRequestHandler<GetAllergenCategoriesQuery, AppResponse<IEnumerable<string>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetAllergenCategoriesQueryHandler> _logger;
    private readonly ICacheKeyService _cacheKeyService;
    private readonly ICacheService _cacheService;
    public GetAllergenCategoriesQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetAllergenCategoriesQueryHandler> logger,
        ICacheKeyService cacheKeyService,
        ICacheService cacheService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _cacheKeyService = cacheKeyService;
        _cacheService = cacheService;
    }

    public async Task<AppResponse<IEnumerable<string>>> Handle(GetAllergenCategoriesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var cacheKey = _cacheKeyService.Custom("allergen", "categories");
            
            // Try to get categories from cache first
            var cachedCategories = await _cacheService.GetAsync<IEnumerable<string>>(cacheKey);
            if (cachedCategories != null)
            {
                _logger.LogInformation("Retrieved allergen categories from cache");
                return new AppResponse<IEnumerable<string>>()
                    .SetSuccessResponse(cachedCategories);
            }

            // If not in cache, fetch from database
            var allergens = await _unitOfWork.Repository<Allergen>().ListAllAsync();
            var categories = allergens
                .Where(a => !string.IsNullOrWhiteSpace(a.Category))
                .Select(a => a.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            // Cache the result for future requests
            await _cacheService.SetAsync(cacheKey, categories, TimeSpan.FromHours(24));
            _logger.LogInformation("Cached allergen categories for 24 hours");

            return new AppResponse<IEnumerable<string>>()
                .SetSuccessResponse(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving allergen categories");
            return new AppResponse<IEnumerable<string>>()
                .SetErrorResponse("GetAllergenCategories", "An error occurred while retrieving allergen categories");
        }
    }
} 