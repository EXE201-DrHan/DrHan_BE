using MediatR;
using FluentValidation;
using DrHan.Application.DTOs.Authentication;
using DrHan.Application.Commons;

namespace DrHan.Application.Services.AuthenticationServices.Commands.ConfirmEmail
{
    public class ConfirmEmailCommand : IRequest<AppResponse<ConfirmEmailResponse>>
    {
        public int UserId { get; set; }
        public string Token { get; set; } = string.Empty;
    }

    public class ConfirmEmailCommandValidator : AbstractValidator<ConfirmEmailCommand>
    {
        public ConfirmEmailCommandValidator()
        {
            RuleFor(x => x.UserId)
                .GreaterThan(0).WithMessage("Valid User ID is required");

            RuleFor(x => x.Token)
                .NotEmpty().WithMessage("Token is required")
                .MinimumLength(10).WithMessage("Invalid token format");
        }
    }
} 