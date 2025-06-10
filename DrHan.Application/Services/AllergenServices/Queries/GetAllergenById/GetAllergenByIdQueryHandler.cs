using MediatR;
using AutoMapper;
using DrHan.Application.DTOs.Allergens;
using DrHan.Application.Commons;
using DrHan.Application.Interfaces.Repository;
using DrHan.Domain.Entities.Allergens;
using Microsoft.Extensions.Logging;

namespace DrHan.Application.Services.AllergenServices.Queries.GetAllergenById;

public class GetAllergenByIdQueryHandler : IRequestHandler<GetAllergenByIdQuery, AppResponse<AllergenDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetAllergenByIdQueryHandler> _logger;

    public GetAllergenByIdQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetAllergenByIdQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<AppResponse<AllergenDto>> Handle(GetAllergenByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var allergen = await _unitOfWork.Repository<Allergen>().FindAsync(a => a.Id == request.Id);
            if (allergen == null)
            {
                return new AppResponse<AllergenDto>()
                    .SetErrorResponse("Id", "Allergen not found");
            }

            var allergenDto = _mapper.Map<AllergenDto>(allergen);

            return new AppResponse<AllergenDto>()
                .SetSuccessResponse(allergenDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving allergen with ID {Id}", request.Id);
            return new AppResponse<AllergenDto>()
                .SetErrorResponse("GetAllergenById", "An error occurred while retrieving the allergen");
        }
    }
} 