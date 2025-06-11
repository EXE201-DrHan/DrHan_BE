using DrHan.Application.Commons;
using DrHan.Application.DTOs.Recipes;
using MediatR;

namespace DrHan.Application.Services.RecipeServices.Queries.SearchRecipes;

public class SearchRecipesQuery : IRequest<AppResponse<IPaginatedList<RecipeDto>>>
{
    public RecipeSearchDto SearchDto { get; set; }

    public SearchRecipesQuery(RecipeSearchDto searchDto)
    {
        SearchDto = searchDto ?? throw new ArgumentNullException(nameof(searchDto));
    }
} 