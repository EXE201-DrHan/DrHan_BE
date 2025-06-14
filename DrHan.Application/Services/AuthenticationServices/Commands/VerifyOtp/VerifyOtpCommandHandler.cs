using MediatR;
using DrHan.Application.Interfaces.Services;
using DrHan.Application.Interfaces.Services.AuthenticationServices;
using DrHan.Application.Commons;
using DrHan.Domain.Entities.Users;
using DrHan.Application.DTOs.Authentication;
using DrHan.Domain.Enums;

namespace DrHan.Application.Services.AuthenticationServices.Commands.VerifyOtp;

public class VerifyOtpCommandHandler : IRequestHandler<VerifyOtpCommand, AppResponse<VerifyOtpResponse>>
{
    private readonly IApplicationUserService<ApplicationUser> _userService;
    private readonly IOtpService _otpService;

    public VerifyOtpCommandHandler(
        IApplicationUserService<ApplicationUser> userService,
        IOtpService otpService)
    {
        _userService = userService;
        _otpService = otpService;
    }

    public async Task<AppResponse<VerifyOtpResponse>> Handle(VerifyOtpCommand request, CancellationToken cancellationToken)
    {
        var user = await _userService.GetByIdAsync(request.UserId);
        if (user == null)
        {
            return new AppResponse<VerifyOtpResponse>()
                .SetErrorResponse("User", "User not found");
        }

        var isValid = await _otpService.ValidateOtpAsync(request.UserId, request.Code, request.Type);
        
        if (!isValid)
        {
            var remainingOtp = await _otpService.GetValidOtpAsync(request.UserId, request.Type);
            var remainingAttempts = remainingOtp?.MaxAttempts - remainingOtp?.AttemptsCount ?? 0;
            
            return new AppResponse<VerifyOtpResponse>()
                .SetSuccessResponse(new VerifyOtpResponse
                {
                    IsVerified = false,
                    Message = "Invalid or expired OTP code",
                    RemainingAttempts = Math.Max(0, remainingAttempts)
                }, "OTP verification failed");
        }

        // If it's email verification, confirm the user's email
        if (request.Type == OtpType.EmailVerification)
        {
            user.EmailConfirmed = true;
            user.UpdatedAt = DateTime.UtcNow;
            await _userService.UpdateAsync(user);
        }

        var response = new VerifyOtpResponse
        {
            IsVerified = true,
            Message = "OTP verified successfully",
            IsEmailConfirmed = user.EmailConfirmed,
            RemainingAttempts = 3
        };

        return new AppResponse<VerifyOtpResponse>()
            .SetSuccessResponse(response, "OTP verified successfully");
    }
} 