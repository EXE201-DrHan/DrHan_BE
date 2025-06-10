using MediatR;
using AutoMapper;
using DrHan.Application.DTOs.Allergens;
using DrHan.Application.Commons;
using DrHan.Application.Interfaces.Repository;
using DrHan.Domain.Entities.Allergens;
using Microsoft.Extensions.Logging;

namespace DrHan.Application.Services.AllergenServices.Queries.GetAllergensByCategory;

public class GetAllergensByCategoryQueryHandler : IRequestHandler<GetAllergensByCategoryQuery, AppResponse<IEnumerable<AllergenDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetAllergensByCategoryQueryHandler> _logger;

    public GetAllergensByCategoryQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetAllergensByCategoryQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<AppResponse<IEnumerable<AllergenDto>>> Handle(GetAllergensByCategoryQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Category))
            {
                return new AppResponse<IEnumerable<AllergenDto>>()
                    .SetErrorResponse("Category", "Category parameter is required");
            }

            var allergens = await _unitOfWork.Repository<Allergen>()
                .ListAsync(a => a.Category.ToLower() == request.Category.ToLower());

            var allergenDtos = _mapper.Map<IEnumerable<AllergenDto>>(allergens);

            return new AppResponse<IEnumerable<AllergenDto>>()
                .SetSuccessResponse(allergenDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving allergens for category {Category}", request.Category);
            return new AppResponse<IEnumerable<AllergenDto>>()
                .SetErrorResponse("GetAllergensByCategory", "An error occurred while retrieving allergens by category");
        }
    }
} 