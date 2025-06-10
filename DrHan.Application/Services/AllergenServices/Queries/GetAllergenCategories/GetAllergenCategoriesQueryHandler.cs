using MediatR;
using DrHan.Application.Commons;
using DrHan.Application.Interfaces.Repository;
using DrHan.Domain.Entities.Allergens;
using Microsoft.Extensions.Logging;

namespace DrHan.Application.Services.AllergenServices.Queries.GetAllergenCategories;

public class GetAllergenCategoriesQueryHandler : IRequestHandler<GetAllergenCategoriesQuery, AppResponse<IEnumerable<string>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetAllergenCategoriesQueryHandler> _logger;

    public GetAllergenCategoriesQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetAllergenCategoriesQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<AppResponse<IEnumerable<string>>> Handle(GetAllergenCategoriesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var allergens = await _unitOfWork.Repository<Allergen>().ListAllAsync();
            var categories = allergens
                .Where(a => !string.IsNullOrWhiteSpace(a.Category))
                .Select(a => a.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

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