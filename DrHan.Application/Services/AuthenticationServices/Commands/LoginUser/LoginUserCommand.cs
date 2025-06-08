using MediatR;
using FluentValidation;
using DrHan.Application.DTOs.Authentication;
using DrHan.Application.Commons;

namespace DrHan.Application.Services.AuthenticationServices.Commands.LoginUser
{
    public class LoginUserCommand : IRequest<AppResponse<LoginUserResponse>>
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginUserCommandValidator : AbstractValidator<LoginUserCommand>
    {
        public LoginUserCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required");
        }
    }
} 