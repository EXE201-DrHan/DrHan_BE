using MediatR;
using DrHan.Application.Interfaces.Services;
using DrHan.Application.Interfaces.Services.AuthenticationServices;
using DrHan.Application.Commons;
using DrHan.Domain.Entities.Users;
using DrHan.Application.DTOs.Authentication;

namespace DrHan.Application.Services.AuthenticationServices.Commands.SendOtp;

public class SendOtpCommandHandler : IRequestHandler<SendOtpCommand, AppResponse<SendOtpResponse>>
{
    private readonly IApplicationUserService<ApplicationUser> _userService;
    private readonly IOtpService _otpService;

    public SendOtpCommandHandler(
        IApplicationUserService<ApplicationUser> userService,
        IOtpService otpService)
    {
        _userService = userService;
        _otpService = otpService;
    }

    public async Task<AppResponse<SendOtpResponse>> Handle(SendOtpCommand request, CancellationToken cancellationToken)
    {
        var user = await _userService.GetByIdAsync(request.UserId);
        if (user == null)
        {
            return new AppResponse<SendOtpResponse>()
                .SetErrorResponse("User", "User not found");
        }

        try
        {
            var otpCode = await _otpService.GenerateOtpAsync(request.UserId, request.Type, request.PhoneNumber);
            
            var response = new SendOtpResponse
            {
                Message = "OTP sent successfully via push notification",
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                RemainingAttempts = 3
            };

            return new AppResponse<SendOtpResponse>()
                .SetSuccessResponse(response, "Success", "OTP sent successfully");
        }
        catch (Exception ex)
        {
            return new AppResponse<SendOtpResponse>()
                .SetErrorResponse("OTP", $"Failed to send OTP: {ex.Message}");
        }
    }
} 