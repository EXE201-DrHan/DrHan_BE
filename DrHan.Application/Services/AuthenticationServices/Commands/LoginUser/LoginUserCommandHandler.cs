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
            
            // Always check password to prevent timing attacks, even if user doesn't exist
            bool isPasswordValid = false;
            if (user != null)
            {
                isPasswordValid = await _userService.CheckPasswordAsync(user, request.Password);
            }
            else
            {
                // Perform a dummy password check to maintain consistent timing
                await _userService.CheckPasswordAsync(new ApplicationUser(), request.Password);
            }
            
            if (user == null || !isPasswordValid)
            {
                return new AppResponse<LoginUserResponse>()
                    .SetErrorResponse("Credentials", "Invalid email or password");
            }

            // Check if email is confirmed (optional security feature)
            var isEmailConfirmed = await _userService.IsEmailConfirmedAsync(user);
            if (!isEmailConfirmed)
            {
                return new AppResponse<LoginUserResponse>()
                    .SetErrorResponse("Email", "Please confirm your email address before logging in");
            }

            // Check if account is locked out
            if (user.LockoutEnabled && user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
            {
                return new AppResponse<LoginUserResponse>()
                    .SetErrorResponse("Account", $"Account is locked until {user.LockoutEnd.Value:yyyy-MM-dd HH:mm:ss} UTC");
            }

            // Check account status
            if (user.Status == DrHan.Domain.Constants.Status.UserStatus.Disabled)
            {
                return new AppResponse<LoginUserResponse>()
                    .SetErrorResponse("Account", "Account has been disabled. Please contact support.");
            }

            // Update last login time
            user.LastLoginAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            await _userService.UpdateAsync(user);

            var role = await _userService.GetUserRoleAsync(user);
            if (string.IsNullOrEmpty(role))
            {
                return new AppResponse<LoginUserResponse>()
                    .SetErrorResponse("Account", "User role not assigned. Please contact support.");
            }

            var accessToken = _tokenService.CreateAccessToken(user, role);
            var refreshToken = _tokenService.CreateRefreshToken(user);
            var tokenExpiration = _tokenService.GetAccessTokenExpiration();

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
                    RefreshToken = refreshToken,
                    TokenExpiresAt = tokenExpiration
                });
        }
    }
} 