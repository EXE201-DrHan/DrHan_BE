using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using DrHan.Application.Services.AuthenticationServices.Commands.LoginUser;
using DrHan.Application.Services.AuthenticationServices.Commands.RegisterUser;
using DrHan.Application.Services.AuthenticationServices.Commands.LogoutUser;
using DrHan.Application.Services.AuthenticationServices.Commands.RefreshToken;
using DrHan.Application.Services.AuthenticationServices.Commands.ConfirmEmail;
using DrHan.Application.Services.AuthenticationServices.Commands.SendPasswordReset;
using DrHan.Application.Services.AuthenticationServices.Commands.ResetPassword;
using DrHan.Application.Services.AuthenticationServices.Commands.RevokeUser;
using DrHan.Application.Services.AuthenticationServices.Commands.SendOtp;
using DrHan.Application.Services.AuthenticationServices.Commands.VerifyOtp;
using DrHan.Application.Services.AuthenticationServices.Commands.ResendOtp;
using DrHan.Application.Services.AuthenticationServices.Commands.ReactivateAccount;
using DrHan.Application.DTOs.Authentication;
using DrHan.Application.Commons;
using System.Security.Claims;
using DrHan.Application.Interfaces.Services;
using DrHan.Domain.Enums;

namespace DrHan.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IPushNotificationService _pushNotificationService;
        private readonly ILogger<AuthenticationController> _logger;
        public AuthenticationController(IMediator mediator, IPushNotificationService pushNotificationService, ILogger<AuthenticationController> logger)
        {
            _mediator = mediator;
            _pushNotificationService = pushNotificationService;
            _logger = logger;
        }

        /// <summary>
        /// Register a new user account
        /// </summary>
        /// <param name="command">User registration details</param>
        /// <returns>Registration response with user details</returns>
        [HttpPost("register")]
        public async Task<ActionResult<AppResponse<RegisterUserResponse>>> Register([FromBody] RegisterUserCommand command)
        {
            var response = await _mediator.Send(command);
            
            if (!response.IsSucceeded)
                return BadRequest(response);
            
            return Ok(response);
        }

        /// <summary>
        /// Login user with email and password
        /// </summary>
        /// <param name="command">Login credentials</param>
        /// <returns>Login response with tokens</returns>
        [HttpPost("login")]
        public async Task<ActionResult<AppResponse<LoginUserResponse>>> Login([FromBody] LoginUserCommand command)
        {
            var response = await _mediator.Send(command);
            
            if (!response.IsSucceeded)
                return BadRequest(response);
            
            return Ok(response);
        }

        /// <summary>
        /// Logout user and revoke tokens
        /// </summary>
        /// <param name="command">User logout details</param>
        /// <returns>Logout confirmation</returns>
        [HttpPost("logout")]
        public async Task<ActionResult<AppResponse<LogoutUserResponse>>> Logout([FromBody] LogoutUserCommand command)
        {
            var response = await _mediator.Send(command);
            
            if (!response.IsSucceeded)
                return BadRequest(response);
            
            return Ok(response);
        }

        /// <summary>
        /// Refresh access token using refresh token
        /// </summary>
        /// <param name="command">Refresh token details</param>
        /// <returns>New access token</returns>
        [HttpPost("refresh-token")]
        public async Task<ActionResult<AppResponse<RefreshTokenResponse>>> RefreshToken([FromBody] RefreshTokenCommand command)
        {
            var response = await _mediator.Send(command);
            
            if (!response.IsSucceeded)
                return BadRequest(response);
            
            return Ok(response);
        }

        /// <summary>
        /// Confirm user email with verification token
        /// </summary>
        /// <param name="command">Email confirmation details</param>
        /// <returns>Email confirmation response</returns>
        [HttpPost("confirm-email")]
        public async Task<ActionResult<AppResponse<ConfirmEmailResponse>>> ConfirmEmail([FromBody] ConfirmEmailCommand command)
        {
            var response = await _mediator.Send(command);
            
            if (!response.IsSucceeded)
                return BadRequest(response);
            
            return Ok(response);
        }

        /// <summary>
        /// Send password reset email to user
        /// </summary>
        /// <param name="command">Password reset request details</param>
        /// <returns>Password reset email confirmation</returns>
        [HttpPost("send-password-reset")]
        public async Task<ActionResult<AppResponse<SendPasswordResetResponse>>> SendPasswordReset([FromBody] SendPasswordResetCommand command)
        {
            var response = await _mediator.Send(command);
            
            if (!response.IsSucceeded)
                return BadRequest(response);
            
            return Ok(response);
        }

        /// <summary>
        /// Reset user password using reset token
        /// </summary>
        /// <param name="command">Password reset details with token</param>
        /// <returns>Password reset confirmation</returns>
        [HttpPost("reset-password")]
        public async Task<ActionResult<AppResponse<ResetPasswordResponse>>> ResetPassword([FromBody] ResetPasswordCommand command)
        {
            var response = await _mediator.Send(command);
            
            if (!response.IsSucceeded)
                return BadRequest(response);
            
            return Ok(response);
        }

        /// <summary>
        /// Revoke user account access
        /// </summary>
        /// <param name="command">User revocation details</param>
        /// <returns>User revocation confirmation</returns>
        [HttpPost("revoke-user")]
        [Authorize(Roles = "Admin")] // Only admins can revoke users
        public async Task<ActionResult<AppResponse<RevokeUserResponse>>> RevokeUser([FromBody] RevokeUserCommand command)
        {
            var response = await _mediator.Send(command);
            
            if (!response.IsSucceeded)
                return BadRequest(response);
            
            return Ok(response);
        }

        /// <summary>
        /// Get current user profile (requires authentication)
        /// </summary>
        /// <returns>Current user information</returns>
        [HttpGet("profile")]
        [Authorize]
        public async Task<ActionResult> GetProfile()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            
            return Ok(new { 
                UserId = userId, 
                Email = email, 
                Role = role,
                Message = "Authentication working correctly!" 
            });
        }

        /// <summary>
        /// Admin-only endpoint for testing role-based authorization
        /// </summary>
        /// <returns>Admin information</returns>
        [HttpGet("admin-only")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> AdminOnly()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            
            return Ok(new { 
                Message = "Admin access granted!", 
                UserId = userId,
                Email = email,
                Timestamp = DateTime.Now 
            });
        }

        /// <summary>
        /// Send OTP to user via push notification
        /// </summary>
        /// <param name="command">OTP sending details</param>
        /// <returns>OTP sending response</returns>
        [HttpPost("send-otp")]
        public async Task<ActionResult<AppResponse<SendOtpResponse>>> SendOtp([FromBody] SendOtpCommand command)
        {
            var response = await _mediator.Send(command);
            
            if (!response.IsSucceeded)
                return BadRequest(response);
            
            return Ok(response);
        }

        /// <summary>
        /// Verify OTP code
        /// </summary>
        /// <param name="command">OTP verification details</param>
        /// <returns>OTP verification response</returns>
        [HttpPost("verify-otp")]
        public async Task<ActionResult<AppResponse<VerifyOtpResponse>>> VerifyOtp([FromBody] VerifyOtpCommand command)
        {
            command.Type = OtpType.EmailVerification;
            var response = await _mediator.Send(command);
            _logger.LogInformation(response.Messages.Keys.ToString());
            if (!response.IsSucceeded)
                return BadRequest(response);
            
            return Ok(response);
        }

        /// <summary>
        /// Resend OTP code to user
        /// </summary>
        /// <param name="command">Resend OTP details</param>
        /// <returns>Resend OTP response</returns>
        [HttpPost("resend-otp")]
        public async Task<ActionResult<AppResponse<ResendOtpResponse>>> ResendOtp([FromBody] ResendOtpCommand command)
        {
            command.Type = OtpType.EmailVerification;
            var response = await _mediator.Send(command);
            
            if (!response.IsSucceeded)
                return BadRequest(response);
            
            return Ok(response);
        }

        /// <summary>
        /// Reactivate abandoned account and send new OTP
        /// </summary>
        /// <param name="command">Account reactivation details</param>
        /// <returns>Account reactivation response</returns>
        [HttpPost("reactivate-account")]
        public async Task<ActionResult<AppResponse<ReactivateAccountResponse>>> ReactivateAccount([FromBody] ReactivateAccountCommand command)
        {
            var response = await _mediator.Send(command);
            
            if (!response.IsSucceeded)
                return BadRequest(response);
            
            return Ok(response);
        }
        
        /// <summary>
        /// Register device token for push notifications
        /// </summary>
        /// <param name="deviceToken">Device token from FCM</param>
        /// <param name="platform">Platform (iOS, Android, Web)</param>
        /// <returns>Registration response</returns>
        [HttpPost("register-device")]
        [Authorize]
        public async Task<ActionResult> RegisterDevice([FromBody] RegisterDeviceRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var userIdInt))
                return Unauthorized();

            var success = await _pushNotificationService.RegisterDeviceTokenAsync(userIdInt, request.DeviceToken, request.Platform);
            
            if (success)
                return Ok(new { Message = "Device registered successfully" });
            else
                return BadRequest(new { Message = "Failed to register device" });
        }
    }
}

public class RegisterDeviceRequest
{
    public string DeviceToken { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
} 