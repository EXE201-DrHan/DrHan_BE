using DrHan.Application.Commons;
using DrHan.Application.Interfaces.Services.AuthenticationServices;
using DrHan.Domain.Constants.Status;
using DrHan.Domain.Entities.Users;
using DrHan.Domain.Exceptions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DrHan.Infrastructure.ExternalServices.AuthenticationService
{
    public class ApplicationUserService<T> : IApplicationUserService<T> where T : ApplicationUser
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;

        public ApplicationUserService(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IReadOnlyList<T>> GetAllUsersAsync()
        {
            return await _userManager.Users.OfType<T>().ToListAsync();
        }

        public async Task<IList<ApplicationUser>> GetUsersByRoleAsync(string role)
        {
            return await _userManager.GetUsersInRoleAsync(role);
        }

        public async Task<IReadOnlyList<T>> GetAllUsersWithFilterAsync(Expression<Func<T, bool>> filter)
        {
            var query = _userManager.Users.OfType<T>().AsQueryable();
            if (filter != null)
            {
                query = query.Where(filter);
            }
            return await query.ToListAsync();
        }

        public async Task<T?> GetUserWithFilterAsync(Expression<Func<T, bool>> filter)
        {
            var query = _userManager.Users.OfType<T>().AsQueryable();
            if (filter != null)
            {
                query = query.Where(filter);
            }
            return await query.FirstOrDefaultAsync();
        }

        public async Task<int> CountAsync(Expression<Func<T, bool>> filter)
        {
            var query = _userManager.Users.OfType<T>().AsQueryable();
            if (filter != null)
            {
                query = query.Where(filter);
            }
            return await query.CountAsync();
        }

        public Task InsertBulkAsync(IEnumerable<(ApplicationUser user, string password)> usersWithPassword)
        {
            throw new NotImplementedException();
        }

        public async Task UpdateAsync(ApplicationUser user)
        {
            await _userManager.UpdateAsync(user);
        }

        public async Task InsertAsync(ApplicationUser user, string password)
        {
            var existingUserByEmail = await _userManager.FindByEmailAsync(user.Email!);
            if (existingUserByEmail != null)
            {
                throw new DuplicateUserException($"A user with the email {user.Email} already exists.");
            }

            var existingUserByUsername = await _userManager.FindByNameAsync(user.UserName!);
            if (existingUserByUsername != null)
            {
                throw new DuplicateUserException($"A user with the username {user.UserName} already exists.");
            }

            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                throw new CreateFailedException(user.Email!);
            }
        }

        public async Task AssignRoleAsync(T user, string role)
        {
            await _userManager.AddToRoleAsync(user, role);
        }

        public async Task<ApplicationUser?> GetUserByEmailAsync(string email)
        {
            return await _userManager.FindByEmailAsync(email);
        }

        public async Task<string> GetUserRoleAsync(T user)
        {
            return (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? string.Empty;
        }

        public async Task<ApplicationUser?> GetByIdAsync(int userId)
        {
            return await _userManager.FindByIdAsync(userId.ToString());
        }

        public async Task ToggleAccountStatusAsync(int userId)
        {
            var user = await GetByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException(nameof(T), userId.ToString());
            }

            user.Status = user.Status == UserStatus.Enabled ? UserStatus.Disabled : UserStatus.Enabled;
            await UpdateAsync(user);
        }

        public async Task<IPaginatedList<ApplicationUser>> FilterUserByRoleAsync(
            string role,
            Expression<Func<ApplicationUser, bool>> filter = null,
            Func<IQueryable<ApplicationUser>, IOrderedQueryable<ApplicationUser>> orderBy = null,
            PaginationRequest pagination = null)
        {
            var query = _userManager.Users.AsQueryable();
            var usersInRole = await _userManager.GetUsersInRoleAsync(role);
            var userIds = usersInRole.Select(u => u.Id).ToList();

            query = query.Where(u => userIds.Contains(u.Id));

            if (filter != null)
            {
                query = query.Where(filter);
            }

            if (orderBy != null)
            {
                query = orderBy(query); 
            }

            var totalCount = await query.CountAsync();

            int pageNumber = 1;
            int pageSize = totalCount;
            if (pagination != null)
            {
                if (pagination.PageNumber < 1)
                    throw new ArgumentOutOfRangeException(nameof(pagination.PageNumber), "Page number must be at least 1.");
                if (pagination.PageSize < 1)
                    throw new ArgumentOutOfRangeException(nameof(pagination.PageSize), "Page size must be at least 1.");

                pageNumber = pagination.PageNumber;
                pageSize = pagination.PageSize;
                query = query.Skip((pagination.PageNumber - 1) * pageSize).Take(pagination.PageSize);
            }
            else if (totalCount > 0)
            {
                pageSize = 10; // Default page size
                query = query.Take(pageSize);
            }

            var pagedUsers = await query.ToListAsync();
            return PaginatedList<ApplicationUser>.Create(
                pagedUsers.AsReadOnly(),
                pageNumber,
                pageSize,
                totalCount);
        }

        public async Task<int> CountUserByRoleAsync(string role, Expression<Func<ApplicationUser, bool>> filter = null)
        {
            var query = _userManager.Users.AsQueryable();
            var usersInRole = await _userManager.GetUsersInRoleAsync(role);
            var userIds = usersInRole.Select(u => u.Id).ToList();

            query = query.Where(u => userIds.Contains(u.Id));

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.CountAsync();
        }
    }
}
