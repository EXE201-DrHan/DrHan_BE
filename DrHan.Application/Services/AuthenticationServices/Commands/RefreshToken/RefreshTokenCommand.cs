using MediatR;
using FluentValidation;
using DrHan.Application.Commons;
using DrHan.Application.DTOs.Authentication;

namespace DrHan.Application.Services.AuthenticationServices.Commands.RefreshToken
{
    public class RefreshTokenCommand : IRequest<AppResponse<RefreshTokenResponse>>
    {
        public string RefreshToken { get; set; } = string.Empty;
        public int UserId { get; set; }
    }

    public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
    {
        public RefreshTokenCommandValidator()
        {
            RuleFor(x => x.RefreshToken)
                .NotEmpty().WithMessage("Refresh token is required");

            RuleFor(x => x.UserId)
                .GreaterThan(0).WithMessage("Valid User ID is required");
        }
    }
} 