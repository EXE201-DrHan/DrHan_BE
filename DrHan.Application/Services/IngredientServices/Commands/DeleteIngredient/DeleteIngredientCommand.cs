using MediatR;
using FluentValidation;
using DrHan.Application.Commons;

namespace DrHan.Application.Services.IngredientServices.Commands.DeleteIngredient;

public class DeleteIngredientCommand : IRequest<AppResponse<bool>>
{
    public int Id { get; set; }
    
    public DeleteIngredientCommand(int id)
    {
        Id = id;
    }
}

public class DeleteIngredientCommandValidator : AbstractValidator<DeleteIngredientCommand>
{
    public DeleteIngredientCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Id must be greater than 0");
    }
} 