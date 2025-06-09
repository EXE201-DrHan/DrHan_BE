using MediatR;
using DrHan.Domain.Entities;
using DrHan.Application.Interfaces;
using DrHan.Application.Interfaces.Services.AuthenticationServices;
using DrHan.Application.Commons;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;
using DrHan.Domain.Entities.Users;
using DrHan.Domain.Constants.Status;
using DrHan.Application.DTOs.Authentication;
using Microsoft.Extensions.Logging;
using DrHan.Domain.Constants;
using DrHan.Domain.Constants.Roles;

namespace DrHan.Application.Services.AuthenticationServices.Commands.RegisterUser
{
    public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, AppResponse<RegisterUserResponse>>
    {
        private readonly IApplicationUserService<ApplicationUser> _userService;
        private readonly IEmailService _emailService;
        private readonly IUserTokenService _tokenService;
        private readonly ILogger<RegisterUserCommandHandler> _logger;

        public RegisterUserCommandHandler(
            IApplicationUserService<ApplicationUser> userService,
            IEmailService emailService,
            IUserTokenService tokenService,
            ILogger<RegisterUserCommandHandler> logger)
        {
            _userService = userService;
            _emailService = emailService;
            _tokenService = tokenService;
            _logger = logger;
        }

        public async Task<AppResponse<RegisterUserResponse>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
        {
            var existingUser = await _userService.GetUserByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return new AppResponse<RegisterUserResponse>()
                    .SetErrorResponse("Email", "User with this email already exists");
            }
            if (!Enum.TryParse<Gender>(request.Gender, true, out var genderEnum))
            {
                return new AppResponse<RegisterUserResponse>()
                    .SetErrorResponse("Gender", "Invalid gender value");
            }
            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FullName = request.FullName,
                DateOfBirth = request.DateOfBirth,
                Gender = genderEnum,
                Status = UserStatus.Enabled,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            try
            {
                await _userService.InsertAsync(user, request.Password);
                await _userService.AssignRoleAsync(user, UserRoles.Customer.ToString());

                var token = await _userService.GenerateEmailConfirmationTokenAsync(user);
                var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
                var confirmationLink = $"https://yourdomain.com/confirm-email?userId={user.Id}&token={encodedToken}";

                await _emailService.SendEmailConfirmationAsync(
                    user.Email!,
                    "Confirm your email",
                    $"Please confirm your email by clicking <a href='{confirmationLink}'>here</a>");

                var role = await _userService.GetUserRoleAsync(user);
                var accessToken = _tokenService.CreateAccessToken(user, role);
                var refreshToken = _tokenService.CreateRefreshToken(user);

                return new AppResponse<RegisterUserResponse>()
                    .SetSuccessResponse(new RegisterUserResponse
                    {
                        UserId = user.Id,
                        Email = user.Email,
                        FullName = user.FullName,
                        ProfileImageUrl = user.ProfileImageUrl,
                        SubscriptionTier = user.SubscriptionTier,
                        SubscriptionStatus = user.SubscriptionStatus,
                        SubscriptionExpiresAt = user.SubscriptionExpiresAt,
                        LastLoginAt = user.LastLoginAt,
                        Token = accessToken,
                        RefreshToken = refreshToken
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new AppResponse<RegisterUserResponse>()
                    .SetErrorResponse("Registration", ex.Message);
            }
        }
    }
}
