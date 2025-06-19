using MediatR;
using FluentValidation;
using DrHan.Application.DTOs.Authentication;
using DrHan.Application.Commons;
using DrHan.Domain.Enums;

namespace DrHan.Application.Services.AuthenticationServices.Commands.ResendOtp;

public class ResendOtpCommand : IRequest<AppResponse<ResendOtpResponse>>
{
    public string Email { get; set; } = string.Empty;
    public OtpType Type { get; set; } = OtpType.EmailVerification;
}

public class ResendOtpCommandValidator : AbstractValidator<ResendOtpCommand>
{
    public ResendOtpCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Valid email address is required");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Valid OTP type is required");
    }
} 