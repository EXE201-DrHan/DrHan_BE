using MediatR;
using FluentValidation;
using DrHan.Application.Commons;

namespace DrHan.Application.Services.AuthenticationServices.Commands.LogoutUser
{
    public class LogoutUserCommand : IRequest<AppResponse<LogoutUserResponse>>
    {
        public int UserId { get; set; }
    }

    public class LogoutUserCommandValidator : AbstractValidator<LogoutUserCommand>
    {
        public LogoutUserCommandValidator()
        {
            RuleFor(x => x.UserId)
                .GreaterThan(0).WithMessage("Valid User ID is required");
        }
    }
} 