using MediatR;
using DrHan.Application.Interfaces.Services.AuthenticationServices;
using DrHan.Application.Commons;
using DrHan.Domain.Entities.Users;
using DrHan.Application.DTOs.Authentication;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace DrHan.Application.Services.AuthenticationServices.Commands.ResetPassword
{
    public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, AppResponse<ResetPasswordResponse>>
    {
        private readonly IApplicationUserService<ApplicationUser> _userService;

        public ResetPasswordCommandHandler(IApplicationUserService<ApplicationUser> userService)
        {
            _userService = userService;
        }

        public async Task<AppResponse<ResetPasswordResponse>> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Get user by ID
                var user = await _userService.GetByIdAsync(request.UserId);
                if (user == null)
                {
                    return new AppResponse<ResetPasswordResponse>()
                        .SetErrorResponse("User", "User not found");
                }

                // Decode the token (it was Base64Url encoded when sent)
                string decodedToken;
                try
                {
                    decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Token));
                }
                catch
                {
                    return new AppResponse<ResetPasswordResponse>()
                        .SetErrorResponse("Token", "Invalid token format");
                }

                // Reset password using Identity's built-in method
                var resetResult = await _userService.ResetPasswordAsync(user, decodedToken, request.NewPassword);
                
                if (!resetResult)
                {
                    return new AppResponse<ResetPasswordResponse>()
                        .SetErrorResponse("Token", "Invalid or expired reset token");
                }

                // Update user's updated timestamp
                user.UpdatedAt = DateTime.UtcNow;
                await _userService.UpdateAsync(user);

                return new AppResponse<ResetPasswordResponse>()
                    .SetSuccessResponse(new ResetPasswordResponse
                    {
                        UserId = user.Id,
                        Email = user.Email!,
                        ResetAt = DateTime.UtcNow,
                        Message = "Password has been reset successfully"
                    });
            }
            catch (Exception ex)
            {
                return new AppResponse<ResetPasswordResponse>()
                    .SetErrorResponse("Reset", $"Failed to reset password: {ex.Message}");
            }
        }
    }
} 