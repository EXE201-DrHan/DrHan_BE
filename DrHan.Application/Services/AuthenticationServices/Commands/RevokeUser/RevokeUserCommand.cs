using MediatR;
using FluentValidation;
using DrHan.Application.Commons;
using DrHan.Application.DTOs.Authentication;

namespace DrHan.Application.Services.AuthenticationServices.Commands.RevokeUser
{
    public class RevokeUserCommand : IRequest<AppResponse<RevokeUserResponse>>
    {
        public int UserId { get; set; }
        public string? Reason { get; set; }
    }

    public class RevokeUserCommandValidator : AbstractValidator<RevokeUserCommand>
    {
        public RevokeUserCommandValidator()
        {
            RuleFor(x => x.UserId)
                .GreaterThan(0).WithMessage("Valid User ID is required");
        }
    }
} 