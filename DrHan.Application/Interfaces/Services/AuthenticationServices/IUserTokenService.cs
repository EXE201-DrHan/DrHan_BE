using DrHan.Domain.Entities.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrHan.Application.Interfaces.Services.AuthenticationServices
{
    public interface IUserTokenService
    {
        /// <summary>
        /// Creates an ID token for the specified user with their role.
        /// </summary>
        /// <param name="user">The application user.</param>
        /// <param name="role">The user's role.</param>
        /// <returns>A string representing the ID token.</returns>
        string CreateIdToken(ApplicationUser user, string role);

        /// <summary>
        /// Creates an access token for the specified user with their role.
        /// </summary>
        /// <param name="user">The application user.</param>
        /// <param name="role">The user's role.</param>
        /// <returns>A string representing the access token.</returns>
        string CreateAccessToken(ApplicationUser user, string role);

        /// <summary>
        /// Creates a refresh token for the specified user.
        /// </summary>
        /// <param name="user">The application user.</param>
        /// <returns>A string representing the refresh token.</returns>
        string CreateRefreshToken(ApplicationUser user);

        /// <summary>
        /// Revokes refresh token for the specified user.
        /// </summary>
        /// <param name="userId">The ID of the user whose tokens should be revoked.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RevokeUserToken(string userId);
    }
}
