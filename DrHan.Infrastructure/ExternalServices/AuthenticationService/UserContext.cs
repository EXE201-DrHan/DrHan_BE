using DrHan.Application.Commons;
using DrHan.Application.Interfaces;
using DrHan.Application.Interfaces.Services.AuthenticationServices;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace DrHan.Infrastructure.ExternalServices.AuthenticationService
{
    internal class UserContext(IHttpContextAccessor httpContextAccessor) : IUserContext
    {
        public CurrentUser? GetCurrentUser()
        {
            var user = httpContextAccessor?.HttpContext?.User ?? throw new InvalidOperationException("User context is not present");

            if (user.Identity == null || !user.Identity.IsAuthenticated)
            {
                return null;
            }

            var userId = user.FindFirst(c => c.Type == ClaimTypes.NameIdentifier)!.Value;
            var email = user.FindFirst(c => c.Type == ClaimTypes.Email)!.Value;
            var roles = user.Claims.Where(c => c.Type == ClaimTypes.Role)!.Select(c => c.Value);

            return new CurrentUser(userId, email, roles);
        }

        public int? GetCurrentUserId()
        {
            var user = httpContextAccessor?.HttpContext.User ?? throw new InvalidOperationException("User context is not present");

            if (!user.Identities.Any() || !user.Identity.IsAuthenticated)
            {
                return null;
            }
            int.TryParse(user.FindFirst(c => c.Type == ClaimTypes.NameIdentifier).Value, out int intUserId);
            return intUserId;
        }
    }
}
