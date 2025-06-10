using MediatR;
using FluentValidation;
using DrHan.Application.Commons;

namespace DrHan.Application.Services.AllergenServices.Commands.DeleteAllergen;

public class DeleteAllergenCommand : IRequest<AppResponse<bool>>
{
    public int Id { get; set; }
}

public class DeleteAllergenCommandValidator : AbstractValidator<DeleteAllergenCommand>
{
    public DeleteAllergenCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Id must be greater than 0");
    }
} 