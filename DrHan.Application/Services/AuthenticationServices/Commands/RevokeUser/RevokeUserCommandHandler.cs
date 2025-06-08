using MediatR;
using DrHan.Application.Interfaces.Services.AuthenticationServices;
using DrHan.Application.Commons;
using DrHan.Domain.Entities.Users;
using DrHan.Application.DTOs.Authentication;
using DrHan.Domain.Constants.Status;

namespace DrHan.Application.Services.AuthenticationServices.Commands.RevokeUser
{
    public class RevokeUserCommandHandler : IRequestHandler<RevokeUserCommand, AppResponse<RevokeUserResponse>>
    {
        private readonly IApplicationUserService<ApplicationUser> _userService;
        private readonly IUserTokenService _tokenService;

        public RevokeUserCommandHandler(
            IApplicationUserService<ApplicationUser> userService,
            IUserTokenService tokenService)
        {
            _userService = userService;
            _tokenService = tokenService;
        }

        public async Task<AppResponse<RevokeUserResponse>> Handle(RevokeUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _userService.GetByIdAsync(request.UserId);
            if (user == null)
            {
                return new AppResponse<RevokeUserResponse>()
                    .SetErrorResponse("User", "User not found");
            }

            if (user.Status == UserStatus.Disabled)
            {
                return new AppResponse<RevokeUserResponse>()
                    .SetErrorResponse("User", "User is already revoked/disabled");
            }

            try
            {
                // Revoke all user tokens
                await _tokenService.RevokeUserToken(user.Id.ToString());

                // Disable user account
                user.Status = UserStatus.Disabled;
                user.UpdatedAt = DateTime.UtcNow;
                await _userService.UpdateAsync(user);

                return new AppResponse<RevokeUserResponse>()
                    .SetSuccessResponse(new RevokeUserResponse
                    {
                        UserId = user.Id,
                        Email = user.Email!,
                        Status = user.Status.ToString(),
                        RevokedAt = DateTime.UtcNow,
                        Reason = request.Reason
                    });
            }
            catch (Exception ex)
            {
                return new AppResponse<RevokeUserResponse>()
                    .SetErrorResponse("Revocation", $"Failed to revoke user: {ex.Message}");
            }
        }
    }
}