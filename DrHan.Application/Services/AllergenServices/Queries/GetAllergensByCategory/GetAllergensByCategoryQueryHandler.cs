using MediatR;
using AutoMapper;
using DrHan.Application.DTOs.Allergens;
using DrHan.Application.Commons;
using DrHan.Application.Interfaces.Repository;
using DrHan.Domain.Entities.Allergens;
using Microsoft.Extensions.Logging;
using DrHan.Application.Interfaces.Services.CacheService;

namespace DrHan.Application.Services.AllergenServices.Queries.GetAllergensByCategory;

public class GetAllergensByCategoryQueryHandler : IRequestHandler<GetAllergensByCategoryQuery, AppResponse<IEnumerable<AllergenDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICacheService _cacheService;
    private readonly ICacheKeyService _cacheKeyService;
    private readonly ILogger<GetAllergensByCategoryQueryHandler> _logger;

    private static readonly TimeSpan DefaultCacheExpiration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan EmptyResultCacheExpiration = TimeSpan.FromMinutes(5);

    public GetAllergensByCategoryQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICacheService cacheService,
        ICacheKeyService cacheKeyService,
        ILogger<GetAllergensByCategoryQueryHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _cacheKeyService = cacheKeyService ?? throw new ArgumentNullException(nameof(cacheKeyService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AppResponse<IEnumerable<AllergenDto>>> Handle(
        GetAllergensByCategoryQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            _logger.LogInformation("Retrieving allergens for category: {Category}", request.Category);

            // Validate input
            var validationResult = ValidateRequest(request);
            if (!validationResult.IsSucceeded)
            {
                return validationResult;
            }

            var normalizedCategory = request.Category.Trim().ToLowerInvariant();
            var cacheKey = _cacheKeyService.CollectionByCategory<Allergen>(normalizedCategory);

            var allergenDtos = await _cacheService.GetAsync(cacheKey, async () =>
            {
                _logger.LogInformation("Cache miss for category: {Category}, fetching from database", request.Category);

                var allergens = await _unitOfWork.Repository<Allergen>()
                    .ListAsync(filter: a => a.Category.ToLower() == normalizedCategory);

                return allergens.Any()
                    ? _mapper.Map<IEnumerable<AllergenDto>>(allergens)
                    : Enumerable.Empty<AllergenDto>();

            }, DefaultCacheExpiration);

            _logger.LogInformation("Successfully retrieved {Count} allergens for category: {Category}",
                allergenDtos.Count(), request.Category);

            return new AppResponse<IEnumerable<AllergenDto>>().SetSuccessResponse(allergenDtos);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Operation was cancelled while retrieving allergens for category: {Category}",
                request.Category);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving allergens for category: {Category}", request.Category);

            return new AppResponse<IEnumerable<AllergenDto>>()
                .SetErrorResponse("GetAllergensByCategory",
                    "An error occurred while retrieving allergens by category");
        }
    }

    private static AppResponse<IEnumerable<AllergenDto>> ValidateRequest(GetAllergensByCategoryQuery request)
    {
        if (string.IsNullOrWhiteSpace(request.Category))
        {
            return new AppResponse<IEnumerable<AllergenDto>>()
                .SetErrorResponse("Category", "Category parameter is required and cannot be empty");
        }

        if (request.Category.Length > 100)
        {
            return new AppResponse<IEnumerable<AllergenDto>>()
                .SetErrorResponse("Category", "Category parameter exceeds maximum allowed length");
        }

        return new AppResponse<IEnumerable<AllergenDto>>().SetSuccessResponse(null);
    }
}