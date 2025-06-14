using MediatR;
using FluentValidation;
using DrHan.Application.DTOs.Ingredients;
using DrHan.Application.Commons;

namespace DrHan.Application.Services.IngredientServices.Queries.GetIngredientById;

public class GetIngredientByIdQuery : IRequest<AppResponse<IngredientDto>>
{
    public int Id { get; set; }
}

public class GetIngredientByIdQueryValidator : AbstractValidator<GetIngredientByIdQuery>
{
    public GetIngredientByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Id must be greater than 0");
    }
} 