using MediatR;
using DrHan.Application.Interfaces.Services.AuthenticationServices;
using DrHan.Application.Commons;
using DrHan.Domain.Entities.Users;
using DrHan.Application.DTOs.Authentication;

namespace DrHan.Application.Services.AuthenticationServices.Commands.RefreshToken
{
    public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AppResponse<RefreshTokenResponse>>
    {
        private readonly IApplicationUserService<ApplicationUser> _userService;
        private readonly IUserTokenService _tokenService;

        public RefreshTokenCommandHandler(
            IApplicationUserService<ApplicationUser> userService,
            IUserTokenService tokenService)
        {
            _userService = userService;
            _tokenService = tokenService;
        }

        public async Task<AppResponse<RefreshTokenResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            var user = await _userService.GetByIdAsync(request.UserId);
            if (user == null)
            {
                return new AppResponse<RefreshTokenResponse>()
                    .SetErrorResponse("User", "User not found");
            }

            // Validate refresh token
            var isValidRefreshToken = await _tokenService.ValidateRefreshToken(user.Id.ToString(), request.RefreshToken);
            if (!isValidRefreshToken)
            {
                return new AppResponse<RefreshTokenResponse>()
                    .SetErrorResponse("Token", "Invalid or expired refresh token");
            }

            // Generate new tokens
            var role = await _userService.GetUserRoleAsync(user);
            var accessToken = _tokenService.CreateAccessToken(user, role);
            var newRefreshToken = _tokenService.CreateRefreshToken(user);
            var tokenExpiration = _tokenService.GetAccessTokenExpiration();

            // Update user's updated timestamp
            user.UpdatedAt = DateTime.Now;
            await _userService.UpdateAsync(user);

            return new AppResponse<RefreshTokenResponse>()
                .SetSuccessResponse(new RefreshTokenResponse
                {
                    UserId = user.Id,
                    Email = user.Email!,
                    FullName = user.FullName!,
                    Token = accessToken,
                    RefreshToken = newRefreshToken,
                    TokenExpiresAt = tokenExpiration
                });
        }
    }
} 