using MediatR;
using AutoMapper;
using DrHan.Application.DTOs.Allergens;
using DrHan.Application.Commons;
using DrHan.Application.Interfaces.Repository;
using DrHan.Domain.Entities.Allergens;
using Microsoft.Extensions.Logging;

namespace DrHan.Application.Services.AllergenServices.Queries.SearchAllergens;

public class SearchAllergensQueryHandler : IRequestHandler<SearchAllergensQuery, AppResponse<IEnumerable<AllergenDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<SearchAllergensQueryHandler> _logger;

    public SearchAllergensQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<SearchAllergensQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<AppResponse<IEnumerable<AllergenDto>>> Handle(SearchAllergensQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var searchTerm = request.SearchTerm.ToLower();
            
            var allergens = await _unitOfWork.Repository<Allergen>()
                .ListAsync(
                    filter: a => a.Name.ToLower().Contains(searchTerm) ||
                                (a.ScientificName != null && a.ScientificName.ToLower().Contains(searchTerm)) ||
                                (a.Description != null && a.Description.ToLower().Contains(searchTerm)),
                    orderBy: null);

            var allergenDtos = _mapper.Map<IEnumerable<AllergenDto>>(allergens);

            return new AppResponse<IEnumerable<AllergenDto>>()
                .SetSuccessResponse(allergenDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching allergens with term {SearchTerm}", request.SearchTerm);
            return new AppResponse<IEnumerable<AllergenDto>>()
                .SetErrorResponse("SearchAllergens", "An error occurred while searching allergens");
        }
    }
} 