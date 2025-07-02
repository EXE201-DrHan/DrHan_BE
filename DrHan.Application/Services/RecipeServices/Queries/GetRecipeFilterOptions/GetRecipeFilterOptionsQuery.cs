using MediatR;
using DrHan.Application.Commons;
using DrHan.Application.DTOs.Recipes;

namespace DrHan.Application.Services.RecipeServices.Queries.GetRecipeFilterOptions;

public record GetRecipeFilterOptionsQuery() : IRequest<AppResponse<RecipeFilterOptionsDto>>; 