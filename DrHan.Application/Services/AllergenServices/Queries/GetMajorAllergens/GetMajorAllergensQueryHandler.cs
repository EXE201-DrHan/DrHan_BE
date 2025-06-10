using MediatR;
using AutoMapper;
using DrHan.Application.DTOs.Allergens;
using DrHan.Application.Commons;
using DrHan.Application.Interfaces.Repository;
using DrHan.Domain.Entities.Allergens;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace DrHan.Application.Services.AllergenServices.Queries.GetMajorAllergens;

public class GetMajorAllergensQueryHandler : IRequestHandler<GetMajorAllergensQuery, AppResponse<IEnumerable<AllergenDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetMajorAllergensQueryHandler> _logger;

    public GetMajorAllergensQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetMajorAllergensQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<AppResponse<IEnumerable<AllergenDto>>> Handle(GetMajorAllergensQuery request, CancellationToken cancellationToken)
    {
        try
        {
            Expression<Func<Allergen, bool>> filter = a => true;

            if (request.IsFdaMajor.HasValue && request.IsEuMajor.HasValue)
            {
                // Both specified - use AND logic
                filter = a => a.IsFdaMajor == request.IsFdaMajor && a.IsEuMajor == request.IsEuMajor;
            }
            else if (request.IsFdaMajor.HasValue)
            {
                // Only FDA specified
                filter = a => a.IsFdaMajor == request.IsFdaMajor;
            }
            else if (request.IsEuMajor.HasValue)
            {
                // Only EU specified
                filter = a => a.IsEuMajor == request.IsEuMajor;
            }

            var allergens = await _unitOfWork.Repository<Allergen>()
                .ListAsync(filter: filter);

            var allergensDto = _mapper.Map<IEnumerable<AllergenDto>>(allergens);

            return new AppResponse<IEnumerable<AllergenDto>>()
                .SetSuccessResponse(allergensDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving major allergens with FDA: {IsFdaMajor}, EU: {IsEuMajor}", 
                request.IsFdaMajor, request.IsEuMajor);
            return new AppResponse<IEnumerable<AllergenDto>>()
                .SetErrorResponse("GetMajorAllergens", "An error occurred while retrieving major allergens");
        }
    }
} 