using MediatR;
using DrHan.Application.Commons;
using DrHan.Application.DTOs.Ingredients;

namespace DrHan.Application.Services.IngredientServices.Commands.AddAllergenToIngredient;

public class AddAllergenToIngredientCommand : IRequest<AppResponse<IngredientAllergenDto>>
{
    public int IngredientId { get; set; }
    public int AllergenId { get; set; }
    public string AllergenType { get; set; } = string.Empty;
} 