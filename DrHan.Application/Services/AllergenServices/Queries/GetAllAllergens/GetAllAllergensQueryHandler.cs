using MediatR;
using AutoMapper;
using DrHan.Application.DTOs.Allergens;
using DrHan.Application.Commons;
using DrHan.Application.Interfaces.Repository;
using DrHan.Domain.Entities.Allergens;
using Microsoft.Extensions.Logging;
using DrHan.Application.Interfaces.Services.CacheService;

namespace DrHan.Application.Services.AllergenServices.Queries.GetAllAllergens;

public class GetAllAllergensQueryHandler : IRequestHandler<GetAllAllergensQuery, AppResponse<IEnumerable<AllergenDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetAllAllergensQueryHandler> _logger;
    private readonly ICacheKeyService _cacheKeyService;
    private readonly ICacheService _cacheService;

    public GetAllAllergensQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetAllAllergensQueryHandler> logger,
        ICacheKeyService cacheKeyService,
        ICacheService cacheService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _cacheKeyService = cacheKeyService;
        _cacheService = cacheService;
    }

    public async Task<AppResponse<IEnumerable<AllergenDto>>> Handle(GetAllAllergensQuery request, CancellationToken cancellationToken)
    {
        try
        {
            //var cacheKey = _cacheKeyService.Collection<Allergen>();
            
            //// Try to get allergens from cache first
            //var cachedAllergens = await _cacheService.GetAsync<IEnumerable<AllergenDto>>(cacheKey);
            //if (cachedAllergens != null)
            //{
            //    _logger.LogInformation("Retrieved all allergens from cache");
            //    return new AppResponse<IEnumerable<AllergenDto>>()
            //        .SetSuccessResponse(cachedAllergens);
            //}

            // If not in cache, fetch from database
            var allergens = await _unitOfWork.Repository<Allergen>().ListAllAsync();
            var allergenDtos = _mapper.Map<IEnumerable<AllergenDto>>(allergens);

            // Cache the result for future requests
            //await _cacheService.SetAsync(cacheKey, allergenDtos, TimeSpan.FromHours(12));
            _logger.LogInformation("Cached all allergens for 12 hours");

            return new AppResponse<IEnumerable<AllergenDto>>()
                .SetSuccessResponse(allergenDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all allergens");
            return new AppResponse<IEnumerable<AllergenDto>>()
                .SetErrorResponse("GetAllAllergens", "An error occurred while retrieving allergens");
        }
    }
} 