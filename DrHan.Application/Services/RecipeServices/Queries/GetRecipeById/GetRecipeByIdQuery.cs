using DrHan.Application.Commons;
using DrHan.Application.DTOs.Recipes;
using MediatR;

namespace DrHan.Application.Services.RecipeServices.Queries.GetRecipeById;

public class GetRecipeByIdQuery : IRequest<AppResponse<RecipeDetailDto>>
{
    public int Id { get; set; }

    public GetRecipeByIdQuery(int id)
    {
        Id = id;
    }
} 