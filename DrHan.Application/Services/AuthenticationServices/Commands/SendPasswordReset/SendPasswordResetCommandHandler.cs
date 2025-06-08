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
                        SentAt = DateTime.UtcNow
                    });
            }

            try
            {
                var token = await _userService.GeneratePasswordResetTokenAsync(user);
                var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
                var resetLink = $"https://yourdomain.com/reset-password?userId={user.Id}&token={encodedToken}";

                await _emailService.SendPasswordResetAsync(
                    user.Email!,
                    "Reset your password",
                    $"Please reset your password by clicking <a href='{resetLink}'>here</a>. This link will expire in 1 hour.");

                return new AppResponse<SendPasswordResetResponse>()
                    .SetSuccessResponse(new SendPasswordResetResponse
                    {
                        Email = request.Email,
                        SentAt = DateTime.UtcNow
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