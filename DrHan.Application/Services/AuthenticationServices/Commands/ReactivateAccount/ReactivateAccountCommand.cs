using MediatR;
using FluentValidation;
using DrHan.Application.DTOs.Authentication;
using DrHan.Application.Commons;

namespace DrHan.Application.Services.AuthenticationServices.Commands.ReactivateAccount;

public class ReactivateAccountCommand : IRequest<AppResponse<ReactivateAccountResponse>>
{
    public string Email { get; set; } = string.Empty;
}

public class ReactivateAccountCommandValidator : AbstractValidator<ReactivateAccountCommand>
{
    public ReactivateAccountCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Valid email address is required");
    }
} 