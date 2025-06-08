using MediatR;
using DrHan.Domain.Entities;
using DrHan.Application.Interfaces.Services.AuthenticationServices;
using DrHan.Application.Commons;
using DrHan.Domain.Entities.Users;
using DrHan.Application.DTOs.Authentication;

namespace DrHan.Application.Services.AuthenticationServices.Commands.LoginUser
{
    public class LoginUserCommandHandler : IRequestHandler<LoginUserCommand, AppResponse<LoginUserResponse>>
    {
        private readonly IApplicationUserService<ApplicationUser> _userService;
        private readonly IUserTokenService _tokenService;

        public LoginUserCommandHandler(
            IApplicationUserService<ApplicationUser> userService,
            IUserTokenService tokenService)
        {
            _userService = userService;
            _tokenService = tokenService;
        }

        public async Task<AppResponse<LoginUserResponse>> Handle(LoginUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _userService.GetUserByEmailAsync(request.Email);
            if (user == null)
            {
                return new AppResponse<LoginUserResponse>()
                    .SetErrorResponse("Email", "User not found");
            }

            if (!await _userService.CheckPasswordAsync(user, request.Password))
            {
                return new AppResponse<LoginUserResponse>()
                    .SetErrorResponse("Password", "Invalid password");
            }

            // Update last login time
            user.LastLoginAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            await _userService.UpdateAsync(user);

            var role = await _userService.GetUserRoleAsync(user);
            var accessToken = _tokenService.CreateAccessToken(user, role);
            var refreshToken = _tokenService.CreateRefreshToken(user);

            return new AppResponse<LoginUserResponse>()
                .SetSuccessResponse(new LoginUserResponse
                {
                    UserId = user.Id,
                    Email = user.Email!,
                    FullName = user.FullName!,
                    ProfileImageUrl = user.ProfileImageUrl,
                    SubscriptionTier = user.SubscriptionTier,
                    SubscriptionStatus = user.SubscriptionStatus,
                    SubscriptionExpiresAt = user.SubscriptionExpiresAt,
                    LastLoginAt = user.LastLoginAt,
                    Token = accessToken,
                    RefreshToken = refreshToken
                });
        }
    }
} 