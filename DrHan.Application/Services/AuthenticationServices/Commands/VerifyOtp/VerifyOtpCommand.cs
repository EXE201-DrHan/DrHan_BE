using MediatR;
using FluentValidation;
using DrHan.Application.DTOs.Authentication;
using DrHan.Application.Commons;
using DrHan.Domain.Enums;

namespace DrHan.Application.Services.AuthenticationServices.Commands.VerifyOtp;

public class VerifyOtpCommand : IRequest<AppResponse<VerifyOtpResponse>>
{
    public int UserId { get; set; }
    public string Code { get; set; } = string.Empty;
    public OtpType Type { get; set; }
}

public class VerifyOtpCommandValidator : AbstractValidator<VerifyOtpCommand>
{
    public VerifyOtpCommandValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("Valid User ID is required");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("OTP code is required")
            .Length(6).WithMessage("OTP code must be 6 digits")
            .Matches(@"^\d{6}$").WithMessage("OTP code must contain only digits");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Valid OTP type is required");
    }
} 