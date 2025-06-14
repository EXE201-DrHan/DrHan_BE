using MediatR;
using DrHan.Application.DTOs.Ingredients;
using DrHan.Application.Commons;

namespace DrHan.Application.Services.IngredientServices.Queries.GetIngredientCategories;

public class GetIngredientCategoriesQuery : IRequest<AppResponse<List<IngredientCategoryDto>>>
{
} 