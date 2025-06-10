using MediatR;
using DrHan.Application.DTOs.Allergens;
using DrHan.Application.Commons;

namespace DrHan.Application.Services.AllergenServices.Queries.GetAllergensByCategory;

public class GetAllergensByCategoryQuery : IRequest<AppResponse<IEnumerable<AllergenDto>>>
{
    public string Category { get; set; }
} 