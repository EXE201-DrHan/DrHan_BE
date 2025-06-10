using MediatR;
using DrHan.Application.DTOs.Allergens;
using DrHan.Application.Commons;

namespace DrHan.Application.Services.AllergenServices.Queries.GetAllAllergens;

public class GetAllAllergensQuery : IRequest<AppResponse<IEnumerable<AllergenDto>>>
{
} 