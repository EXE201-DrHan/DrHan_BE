using MediatR;
using DrHan.Application.Interfaces.Services;
using DrHan.Application.Interfaces.Services.AuthenticationServices;
using DrHan.Application.Interfaces;
using DrHan.Application.Commons;
using DrHan.Domain.Entities.Users;
using DrHan.Application.DTOs.Authentication;
using DrHan.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace DrHan.Application.Services.AuthenticationServices.Commands.ResendOtp;

public class ResendOtpCommandHandler : IRequestHandler<ResendOtpCommand, AppResponse<ResendOtpResponse>>
{
    private readonly IApplicationUserService<ApplicationUser> _userService;
    private readonly IOtpService _otpService;
    private readonly IEmailService _emailService;
    private readonly ILogger<ResendOtpCommandHandler> _logger;

    public ResendOtpCommandHandler(
        IApplicationUserService<ApplicationUser> userService,
        IOtpService otpService,
        IEmailService emailService,
        ILogger<ResendOtpCommandHandler> logger)
    {
        _userService = userService;
        _otpService = otpService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<AppResponse<ResendOtpResponse>> Handle(ResendOtpCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Get user by email
            var user = await _userService.GetUserByEmailAsync(request.Email);

            if (user == null)
            {
                // Don't reveal that account doesn't exist for security reasons
                var secureResponse = new ResendOtpResponse
                {
                    IsSuccess = false,
                    Message = "If an account with this email exists, an OTP has been sent to your email address",
                    ExpiresAt = DateTime.Now.AddMinutes(5),
                    RemainingAttempts = 3
                };

                return new AppResponse<ResendOtpResponse>()
                    .SetSuccessResponse(secureResponse, "Info", "OTP resend request processed");
            }

            // Check if user already has verified email (for email verification type)
            if (request.Type == OtpType.EmailVerification && user.EmailConfirmed)
            {
                return new AppResponse<ResendOtpResponse>()
                    .SetErrorResponse("AlreadyVerified", "Email is already verified");
            }

            // Check rate limiting - prevent spam
            var existingOtp = await _otpService.GetValidOtpAsync(user.Id, request.Type);
            if (existingOtp != null)
            {
                var timeSinceLastOtp = DateTime.Now - existingOtp.CreateAt;
                if (timeSinceLastOtp < TimeSpan.FromMinutes(1)) // Minimum 1 minute between requests
                {
                    var waitTime = TimeSpan.FromMinutes(1) - timeSinceLastOtp;
                    return new AppResponse<ResendOtpResponse>()
                        .SetErrorResponse("RateLimit", $"Please wait {waitTime.Seconds} seconds before requesting another OTP");
                }
            }

            // Generate new OTP
            var otpCode = await _otpService.GenerateOtpAsync(user.Id, request.Type);
            
            // Send OTP via email
            await _emailService.SendOtpAsync(user.Email, user.FullName, otpCode);

            _logger.LogInformation("OTP resent successfully for user {UserId} with type {OtpType}", user.Id, request.Type);

            var response = new ResendOtpResponse
            {
                IsSuccess = true,
                Message = "OTP has been sent to your email address",
                ExpiresAt = DateTime.Now.AddMinutes(5),
                RemainingAttempts = 3
            };

            return new AppResponse<ResendOtpResponse>()
                .SetSuccessResponse(response, "Success", "OTP resent successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resending OTP for email {Email}", request.Email);
            return new AppResponse<ResendOtpResponse>()
                .SetErrorResponse("Error", "An error occurred while resending OTP");
        }
    }
} 