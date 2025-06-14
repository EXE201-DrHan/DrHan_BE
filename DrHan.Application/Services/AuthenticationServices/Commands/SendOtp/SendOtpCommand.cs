using MediatR;
using FluentValidation;
using DrHan.Application.DTOs.Authentication;
using DrHan.Application.Commons;
using DrHan.Domain.Enums;

namespace DrHan.Application.Services.AuthenticationServices.Commands.SendOtp;

public class SendOtpCommand : IRequest<AppResponse<SendOtpResponse>>
{
    public int UserId { get; set; }
    public OtpType Type { get; set; }
    public string? PhoneNumber { get; set; }
}

public class SendOtpCommandValidator : AbstractValidator<SendOtpCommand>
{
    public SendOtpCommandValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("Valid User ID is required");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Valid OTP type is required");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().When(x => x.Type == OtpType.PhoneVerification)
            .WithMessage("Phone number is required for phone verification");
    }
} 