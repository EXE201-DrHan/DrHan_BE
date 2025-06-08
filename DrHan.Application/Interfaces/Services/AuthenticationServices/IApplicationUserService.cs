using DrHan.Application.Commons;
using DrHan.Domain.Entities.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DrHan.Application.Interfaces.Services.AuthenticationServices
{
    public interface IApplicationUserService<T> where T : ApplicationUser
    {
        Task<IReadOnlyList<T>> GetAllUsersAsync();
        Task<IList<ApplicationUser>> GetUsersByRoleAsync(string role);
        Task<IReadOnlyList<T>> GetAllUsersWithFilterAsync(Expression<Func<T, bool>> filter);
        Task<T?> GetUserWithFilterAsync(Expression<Func<T, bool>> filter);
        Task<int> CountAsync(Expression<Func<T, bool>> filter);
        Task InsertBulkAsync(IEnumerable<(ApplicationUser user, string password)> usersWithPassword);
        Task UpdateAsync(ApplicationUser user);
        Task InsertAsync(ApplicationUser user, string password);
        Task AssignRoleAsync(T user, string role);
        Task<ApplicationUser?> GetUserByEmailAsync(string email);
        Task<string> GetUserRoleAsync(T user);
        Task<ApplicationUser?> GetByIdAsync(int userId);
        Task ToggleAccountStatusAsync(int userId);
        Task<IPaginatedList<ApplicationUser>> FilterUserByRoleAsync(
            string role,
            Expression<Func<ApplicationUser, bool>> filter = null,
            Func<IQueryable<ApplicationUser>, IOrderedQueryable<ApplicationUser>> orderBy = null,
            PaginationRequest pagination = null);
        Task<int> CountUserByRoleAsync(string role, Expression<Func<ApplicationUser, bool>> filter = null);
        Task<bool> CheckPasswordAsync(ApplicationUser user, string password);
        Task<string> GenerateEmailConfirmationTokenAsync(ApplicationUser user);
        Task<bool> ConfirmEmailAsync(ApplicationUser user, string token);
        Task<string> GeneratePasswordResetTokenAsync(ApplicationUser user);
        Task<bool> ResetPasswordAsync(ApplicationUser user, string token, string newPassword);
        Task<bool> IsEmailConfirmedAsync(ApplicationUser user);
    }
}
