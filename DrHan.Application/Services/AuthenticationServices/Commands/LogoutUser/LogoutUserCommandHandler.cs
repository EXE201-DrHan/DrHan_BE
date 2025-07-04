using MediatR;
using DrHan.Application.Interfaces.Services.AuthenticationServices;
using DrHan.Application.Commons;
using DrHan.Domain.Entities.Users;
using DrHan.Application.DTOs.Authentication;

namespace DrHan.Application.Services.AuthenticationServices.Commands.LogoutUser
{
    public class LogoutUserCommandHandler : IRequestHandler<LogoutUserCommand, AppResponse<LogoutUserResponse>>
    {
        private readonly IApplicationUserService<ApplicationUser> _userService;
        private readonly IUserTokenService _tokenService;

        public LogoutUserCommandHandler(
            IApplicationUserService<ApplicationUser> userService,
            IUserTokenService tokenService)
        {
            _userService = userService;
            _tokenService = tokenService;
        }

        public async Task<AppResponse<LogoutUserResponse>> Handle(LogoutUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _userService.GetByIdAsync(request.UserId);
            if (user == null)
            {
                return new AppResponse<LogoutUserResponse>()
                    .SetErrorResponse("User", "User not found");
            }

            // Revoke user tokens
            await _tokenService.RevokeUserToken(user.Id.ToString());

            // Update user's updated timestamp
            user.UpdatedAt = DateTime.Now;
            await _userService.UpdateAsync(user);

            return new AppResponse<LogoutUserResponse>()
                .SetSuccessResponse(new LogoutUserResponse
                {
                    UserId = user.Id,
                    Email = user.Email!,
                    LoggedOutAt = DateTime.Now
                });
        }
    }
} 