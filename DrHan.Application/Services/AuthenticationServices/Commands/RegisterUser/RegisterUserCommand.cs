using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using FluentValidation;
using DrHan.Application.DTOs.Authentication;
using DrHan.Application.Commons;

namespace DrHan.Application.Services.AuthenticationServices.Commands.RegisterUser
{
    public class RegisterUserCommand : IRequest<AppResponse<RegisterUserResponse>>
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public string ConfirmEmail { get; set; } = string.Empty;
        public string FullName {  get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string Gender {  get; set; } = string.Empty;
    }

    public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
    {
        public RegisterUserCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format");

            RuleFor(x => x.ConfirmEmail)
                .NotEmpty().WithMessage("Confirm email is required")
                .Equal(x => x.Email).WithMessage("Emails do not match");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(6).WithMessage("Password must be at least 6 characters");

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage("Confirm password is required")
                .Equal(x => x.Password).WithMessage("Passwords do not match");

        }
    }
}
