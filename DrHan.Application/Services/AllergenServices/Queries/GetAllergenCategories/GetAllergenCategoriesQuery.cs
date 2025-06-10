using MediatR;
using DrHan.Application.Commons;

namespace DrHan.Application.Services.AllergenServices.Queries.GetAllergenCategories;

public class GetAllergenCategoriesQuery : IRequest<AppResponse<IEnumerable<string>>>
{
} 