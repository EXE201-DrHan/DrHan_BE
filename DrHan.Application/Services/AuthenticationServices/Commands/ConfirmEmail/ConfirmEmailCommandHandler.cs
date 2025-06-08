using MediatR;
using DrHan.Application.Interfaces.Services.AuthenticationServices;
using DrHan.Application.Commons;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;
using DrHan.Domain.Entities.Users;
using DrHan.Application.DTOs.Authentication;

namespace DrHan.Application.Services.AuthenticationServices.Commands.ConfirmEmail
{
    public class ConfirmEmailCommandHandler : IRequestHandler<ConfirmEmailCommand, AppResponse<ConfirmEmailResponse>>
    {
        private readonly IApplicationUserService<ApplicationUser> _userService;
        private readonly IUserTokenService _tokenService;

        public ConfirmEmailCommandHandler(
            IApplicationUserService<ApplicationUser> userService,
            IUserTokenService tokenService)
        {
            _userService = userService;
            _tokenService = tokenService;
        }

        public async Task<AppResponse<ConfirmEmailResponse>> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
        {
            var user = await _userService.GetByIdAsync(request.UserId);
            if (user == null)
            {
                return new AppResponse<ConfirmEmailResponse>()
                    .SetErrorResponse("User", "User not found");
            }

            var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Token));
            var result = await _userService.ConfirmEmailAsync(user, decodedToken);

            if (!result)
            {
                return new AppResponse<ConfirmEmailResponse>()
                    .SetErrorResponse("Email", "Failed to confirm email");
            }

            // Update user's updated timestamp
            user.UpdatedAt = DateTime.UtcNow;
            await _userService.UpdateAsync(user);

            var role = await _userService.GetUserRoleAsync(user);
            var accessToken = _tokenService.CreateAccessToken(user, role);
            var refreshToken = _tokenService.CreateRefreshToken(user);

            return new AppResponse<ConfirmEmailResponse>()
                .SetSuccessResponse(new ConfirmEmailResponse
                {
                    UserId = user.Id,
                    Email = user.Email!,
                    FullName = user.FullName!,
                    Token = accessToken,
                    RefreshToken = refreshToken
                });
        }
    }
} 