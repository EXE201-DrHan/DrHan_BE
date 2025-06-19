using MediatR;
using DrHan.Application.Interfaces.Services;
using DrHan.Application.Interfaces.Services.AuthenticationServices;
using DrHan.Application.Commons;
using DrHan.Domain.Entities.Users;
using DrHan.Application.DTOs.Authentication;
using DrHan.Domain.Enums;
using Microsoft.Extensions.Logging;
using DrHan.Application.Interfaces;

namespace DrHan.Application.Services.AuthenticationServices.Commands.ReactivateAccount;

public class ReactivateAccountCommandHandler : IRequestHandler<ReactivateAccountCommand, AppResponse<ReactivateAccountResponse>>
{
    private readonly IApplicationUserService<ApplicationUser> _userService;
    private readonly IOtpService _otpService;
    private readonly IEmailService _emailService;
    private readonly IUserTokenService _tokenService;
    private readonly ILogger<ReactivateAccountCommandHandler> _logger;

    public ReactivateAccountCommandHandler(
        IApplicationUserService<ApplicationUser> userService,
        IOtpService otpService,
        IEmailService emailService,
        IUserTokenService tokenService,
        ILogger<ReactivateAccountCommandHandler> logger)
    {
        _userService = userService;
        _otpService = otpService;
        _emailService = emailService;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<AppResponse<ReactivateAccountResponse>> Handle(ReactivateAccountCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userService.GetUserByEmailAsync(request.Email);
            
            var response = new ReactivateAccountResponse
            {
                AccountExists = user != null
            };

            if (user == null)
            {
                // Don't reveal that account doesn't exist for security reasons
                response.IsSuccess = false;
                response.Message = "If an account with this email exists, an OTP has been sent to your email address";
                
                return new AppResponse<ReactivateAccountResponse>()
                    .SetSuccessResponse(response, "Info", "Account reactivation request processed");
            }

            // Check if account is already verified
            if (user.EmailConfirmed)
            {
                response.IsSuccess = false;
                response.IsAlreadyVerified = true;
                response.Message = "Account is already verified. You can proceed to login";
                response.UserId = user.Id;

                return new AppResponse<ReactivateAccountResponse>()
                    .SetSuccessResponse(response, "AlreadyVerified", "Account is already verified");
            }

            // Check if user has disabled/locked account
            if (user.Status != Domain.Constants.Status.UserStatus.Enabled)
            {
                response.IsSuccess = false;
                response.Message = "Account is disabled. Please contact support";
                
                return new AppResponse<ReactivateAccountResponse>()
                    .SetErrorResponse("AccountDisabled", "Account is disabled");
            }

            // Check rate limiting for account reactivation
            var existingOtp = await _otpService.GetValidOtpAsync(user.Id, OtpType.EmailVerification);
            if (existingOtp != null)
            {
                var timeSinceLastOtp = DateTime.UtcNow - existingOtp.CreateAt;
                if (timeSinceLastOtp < TimeSpan.FromMinutes(2)) // Minimum 2 minutes between reactivation requests
                {
                    var waitTime = TimeSpan.FromMinutes(2) - timeSinceLastOtp;
                    response.IsSuccess = false;
                    response.Message = $"Please wait {Math.Ceiling(waitTime.TotalMinutes)} minutes before requesting account reactivation again";
                    response.UserId = user.Id;
                    
                    return new AppResponse<ReactivateAccountResponse>()
                        .SetErrorResponse("RateLimit", response.Message);
                }
            }

            // Generate new OTP for email verification
            var otpCode = await _otpService.GenerateOtpAsync(user.Id, OtpType.EmailVerification);
            
            // Send OTP via email
            await _emailService.SendOtpAsync(user.Email, user.FullName, otpCode);

            // Generate tokens for the user to continue the verification flow
            var role = await _userService.GetUserRoleAsync(user);
            var accessToken = _tokenService.CreateAccessToken(user, role);
            var refreshToken = _tokenService.CreateRefreshToken(user);

            // Store refresh token on user entity
            user.RefreshToken = refreshToken;
            user.UpdatedAt = DateTime.UtcNow;
            await _userService.UpdateAsync(user);

            response.IsSuccess = true;
            response.Message = "Account reactivation OTP has been sent to your email address";
            response.UserId = user.Id;
            response.OtpExpiresAt = DateTime.UtcNow.AddMinutes(5);
            response.AccessToken = accessToken;
            response.RefreshToken = refreshToken;

            _logger.LogInformation("Account reactivation OTP sent for user {UserId} with email {Email}", user.Id, user.Email);

            return new AppResponse<ReactivateAccountResponse>()
                .SetSuccessResponse(response, "Success", "Account reactivation OTP sent successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reactivating account for email {Email}", request.Email);
            return new AppResponse<ReactivateAccountResponse>()
                .SetErrorResponse("Error", "An error occurred while reactivating account");
        }
    }
} 