using MediatR;
using FluentValidation;
using DrHan.Application.Commons;
using DrHan.Application.DTOs.Authentication;

namespace DrHan.Application.Services.AuthenticationServices.Commands.SendPasswordReset
{
    public class SendPasswordResetCommand : IRequest<AppResponse<SendPasswordResetResponse>>
    {
        public string Email { get; set; } = string.Empty;
    }

    public class SendPasswordResetCommandValidator : AbstractValidator<SendPasswordResetCommand>
    {
        public SendPasswordResetCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format");
        }
    }
} 