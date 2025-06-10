using MediatR;
using AutoMapper;
using DrHan.Application.DTOs.Allergens;
using DrHan.Application.Commons;
using DrHan.Application.Interfaces.Repository;
using DrHan.Domain.Entities.Allergens;
using Microsoft.Extensions.Logging;

namespace DrHan.Application.Services.AllergenServices.Queries.GetAllAllergens;

public class GetAllAllergensQueryHandler : IRequestHandler<GetAllAllergensQuery, AppResponse<IEnumerable<AllergenDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetAllAllergensQueryHandler> _logger;

    public GetAllAllergensQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetAllAllergensQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<AppResponse<IEnumerable<AllergenDto>>> Handle(GetAllAllergensQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var allergens = await _unitOfWork.Repository<Allergen>().ListAllAsync();
            var allergenDtos = _mapper.Map<IEnumerable<AllergenDto>>(allergens);

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