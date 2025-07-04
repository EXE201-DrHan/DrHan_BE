using MediatR;
using DrHan.Application.Interfaces.Services.AuthenticationServices;
using DrHan.Application.Interfaces;
using DrHan.Application.Commons;
using DrHan.Domain.Entities.Users;
using DrHan.Application.DTOs.Authentication;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace DrHan.Application.Services.AuthenticationServices.Commands.SendPasswordReset
{
    public class SendPasswordResetCommandHandler : IRequestHandler<SendPasswordResetCommand, AppResponse<SendPasswordResetResponse>>
    {
        private readonly IApplicationUserService<ApplicationUser> _userService;
        private readonly IEmailService _emailService;

        public SendPasswordResetCommandHandler(
            IApplicationUserService<ApplicationUser> userService,
            IEmailService emailService)
        {
            _userService = userService;
            _emailService = emailService;
        }

        public async Task<AppResponse<SendPasswordResetResponse>> Handle(SendPasswordResetCommand request, CancellationToken cancellationToken)
        {
            var user = await _userService.GetUserByEmailAsync(request.Email);
            if (user == null)
            {
                return new AppResponse<SendPasswordResetResponse>()
                    .SetSuccessResponse(new SendPasswordResetResponse
                    {
                        Email = request.Email,
                        SentAt = DateTime.Now
                    });
            }

            try
            {
                var token = await _userService.GeneratePasswordResetTokenAsync(user);
                var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
                
                // You can customize this URL to point to your frontend
                var resetLink = $"https://localhost:3000/reset-password?userId={user.Id}&token={encodedToken}";

                var emailBody = $@"
                    <h2>Password Reset Request</h2>
                    <p>Hello {user.FullName ?? user.Email},</p>
                    <p>You have requested to reset your password. Please click the link below to reset your password:</p>
                    <p><a href='{resetLink}' style='background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Reset Password</a></p>
                    <p>If you did not request this password reset, please ignore this email.</p>
                    <p><strong>This link will expire in 1 hour for security purposes.</strong></p>
                    <br>
                    <p>Best regards,<br>DrHan Team</p>
                ";

                await _emailService.SendPasswordResetAsync(
                    user.Email!,
                    "Reset your password - DrHan",
                    emailBody);

                return new AppResponse<SendPasswordResetResponse>()
                    .SetSuccessResponse(new SendPasswordResetResponse
                    {
                        Email = request.Email,
                        SentAt = DateTime.Now
                    });
            }
            catch (Exception ex)
            {
                return new AppResponse<SendPasswordResetResponse>()
                    .SetErrorResponse("Email", $"Failed to send password reset email: {ex.Message}");
            }
        }
    }
} 