using MediatR;
using FluentValidation;
using DrHan.Application.Commons;

namespace DrHan.Application.Services.UserAllergyServices.Commands.RemoveUserAllergy;

public class RemoveUserAllergyCommand : IRequest<AppResponse<bool>>
{
    public int UserId { get; set; }
    public int UserAllergyId { get; set; }
}

public class RemoveUserAllergyCommandValidator : AbstractValidator<RemoveUserAllergyCommand>
{
    public RemoveUserAllergyCommandValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("User ID must be greater than 0");

        RuleFor(x => x.UserAllergyId)
            .GreaterThan(0).WithMessage("User Allergy ID must be greater than 0");
    }
} 