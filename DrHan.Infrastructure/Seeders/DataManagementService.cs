using DrHan.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using DrHan.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace DrHan.Infrastructure.Seeders
{
    public class DataManagementService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DataManagementService> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;

        public DataManagementService(
            ApplicationDbContext context, 
            ILogger<DataManagementService> logger,
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        #region Seeding Operations

        /// <summary>
        /// Seeds all data (users, roles, allergens, ingredients, and their relationships)
        /// </summary>
        public async Task SeedAllDataAsync()
        {
            _logger.LogInformation("Starting complete data seeding...");
            await SeedUsersAndRolesAsync();
            await MasterSeeder.SeedAllAsync(_context, _logger);
        }

        /// <summary>
        /// Seeds users and roles
        /// </summary>
        public async Task SeedUsersAndRolesAsync()
        {
            try
            {
                _logger.LogInformation("Starting user and role seeding...");

                // Validate that managers are not null
                if (_roleManager == null)
                    throw new InvalidOperationException("RoleManager is null. Ensure Identity services are properly configured.");

                if (_userManager == null)
                    throw new InvalidOperationException("UserManager is null. Ensure Identity services are properly configured.");

                await UserSeeder.SeedRolesAndUsersAsync(_roleManager, _userManager);
                _logger.LogInformation("User and role seeding completed!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to seed users and roles");
                throw;
            }
        }

        /// <summary>
        /// Seeds only allergen-related data
        /// </summary>
        public async Task SeedAllergenDataAsync()
        {
            _logger.LogInformation("Starting allergen data seeding...");
            await MasterSeeder.SeedAllergenDataAsync(_context, _logger);
        }

        /// <summary>
        /// Seeds only ingredient-related data
        /// </summary>
        public async Task SeedIngredientDataAsync()
        {
            _logger.LogInformation("Starting ingredient data seeding...");
            await MasterSeeder.SeedIngredientDataAsync(_context, _logger);
        }

        /// <summary>
        /// Seeds only food-related data (allergens and ingredients, but not users)
        /// </summary>
        public async Task SeedFoodDataAsync()
        {
            _logger.LogInformation("Starting food data seeding...");
            await MasterSeeder.SeedAllAsync(_context, _logger);
        }

        #endregion

        #region Cleaning Operations

        /// <summary>
        /// Cleans all seeded data from the database (including users and roles)
        /// </summary>
        public async Task CleanAllDataAsync()
        {
            _logger.LogInformation("Starting complete data cleaning...");
            await CleanUsersAndRolesAsync();
            await DataCleaner.CleanAllAsync(_context, _logger);
        }

        /// <summary>
        /// Cleans only food-related data (allergens and ingredients, but not users)
        /// </summary>
        public async Task CleanFoodDataAsync()
        {
            _logger.LogInformation("Starting food data cleaning...");
            await DataCleaner.CleanAllAsync(_context, _logger);
        }

        /// <summary>
        /// Cleans seeded users and roles (keeps admin)
        /// </summary>
        public async Task CleanUsersAndRolesAsync()
        {
            _logger.LogInformation("Starting user and role cleaning...");
            
            // Get all seeded users (except admin)
            var usersToDelete = _userManager.Users
                .Where(u => u.Email != "admin@example.com")
                .ToList();

            foreach (var user in usersToDelete)
            {
                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    _logger.LogWarning("Failed to delete user {Email}: {Errors}", 
                        user.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }

            _logger.LogInformation("User and role cleaning completed!");
        }

        /// <summary>
        /// Cleans only allergen-related data
        /// </summary>
        public async Task CleanAllergenDataAsync()
        {
            _logger.LogInformation("Starting allergen data cleaning...");
            await DataCleaner.CleanAllergenDataAsync(_context, _logger);
        }

        /// <summary>
        /// Cleans only ingredient-related data
        /// </summary>
        public async Task CleanIngredientDataAsync()
        {
            _logger.LogInformation("Starting ingredient data cleaning...");
            await DataCleaner.CleanIngredientDataAsync(_context, _logger);
        }

        #endregion

        #region Combined Operations

        /// <summary>
        /// Resets all data (cleans and then reseeds including users)
        /// </summary>
        public async Task ResetAllDataAsync()
        {
            _logger.LogInformation("Starting complete data reset...");
            await CleanAllDataAsync();
            await SeedAllDataAsync();
        }

        /// <summary>
        /// Resets food data only (allergens and ingredients, not users)
        /// </summary>
        public async Task ResetFoodDataAsync()
        {
            _logger.LogInformation("Starting food data reset...");
            await CleanFoodDataAsync();
            await SeedFoodDataAsync();
        }

        /// <summary>
        /// Resets users and roles only
        /// </summary>
        public async Task ResetUsersAndRolesAsync()
        {
            _logger.LogInformation("Starting user and role reset...");
            await CleanUsersAndRolesAsync();
            await SeedUsersAndRolesAsync();
        }

        /// <summary>
        /// Resets allergen data only
        /// </summary>
        public async Task ResetAllergenDataAsync()
        {
            _logger.LogInformation("Starting allergen data reset...");
            await CleanAllergenDataAsync();
            await SeedAllergenDataAsync();
        }

        /// <summary>
        /// Resets ingredient data only
        /// </summary>
        public async Task ResetIngredientDataAsync()
        {
            _logger.LogInformation("Starting ingredient data reset...");
            await CleanIngredientDataAsync();
            await SeedIngredientDataAsync();
        }

        #endregion

        #region Statistics and Validation

        /// <summary>
        /// Gets current database statistics
        /// </summary>
        public async Task<DataCleaner.DataStatistics> GetDataStatisticsAsync()
        {
            return await DataCleaner.GetDataStatisticsAsync(_context);
        }

        /// <summary>
        /// Validates JSON data integrity
        /// </summary>
        public async Task<DataValidationHelper.ValidationResult> ValidateJsonDataAsync()
        {
            return await DataValidationHelper.ValidateAllJsonDataAsync();
        }

        /// <summary>
        /// Prints current database statistics to the logger
        /// </summary>
        public async Task LogDataStatisticsAsync()
        {
            var stats = await GetDataStatisticsAsync();
            _logger.LogInformation(stats.ToString());
        }

        /// <summary>
        /// Checks if database has any seeded data
        /// </summary>
        public async Task<bool> HasAnyDataAsync()
        {
            var stats = await GetDataStatisticsAsync();
            var hasUsers = await HasUsersAsync();
            return stats.TotalRecords > 0 || hasUsers;
        }

        /// <summary>
        /// Checks if database has complete seeded data
        /// </summary>
        public async Task<bool> HasCompleteDataAsync()
        {
            var stats = await GetDataStatisticsAsync();
            return stats.CrossReactivityGroupsCount > 0 &&
                   stats.AllergensCount > 0 &&
                   stats.AllergenNamesCount > 0 &&
                   stats.IngredientsCount > 0 &&
                   stats.IngredientNamesCount > 0;
        }

        /// <summary>
        /// Checks if database has seeded users
        /// </summary>
        public async Task<bool> HasUsersAsync()
        {
            var userCount = await Task.FromResult(_userManager.Users.Count());
            return userCount > 0;
        }

        /// <summary>
        /// Gets user statistics
        /// </summary>
        public async Task<UserStatistics> GetUserStatisticsAsync()
        {
            // Get total user count without loading all users into memory
            var totalUsers = await _userManager.Users.CountAsync();

            // Get total roles count
            var totalRoles = await _roleManager.Roles.CountAsync();

            // Get distinct user counts per role
            var roleCounts = await (from user in _userManager.Users
                                    join userRole in _context.UserRoles on user.Id equals userRole.UserId
                                    join role in _roleManager.Roles on userRole.RoleId equals role.Id
                                    group user by role.Name into roleGroup
                                    select new { RoleName = roleGroup.Key, Count = roleGroup.Select(u => u.Id).Distinct().Count() })
                                   .ToListAsync();

            // Extract counts for specific roles
            var adminCount = roleCounts.FirstOrDefault(x => x.RoleName == "Admin")?.Count ?? 0;
            var staffCount = roleCounts.FirstOrDefault(x => x.RoleName == "Staff")?.Count ?? 0;
            var nutritionistCount = roleCounts.FirstOrDefault(x => x.RoleName == "Nutritionist")?.Count ?? 0;
            var customerCount = roleCounts.FirstOrDefault(x => x.RoleName == "Customer")?.Count ?? 0;

            var stats = new UserStatistics
            {
                TotalUsers = totalUsers,
                AdminUsers = adminCount,
                StaffUsers = staffCount,
                NutritionistUsers = nutritionistCount,
                CustomerUsers = customerCount,
                TotalRoles = totalRoles
            };

            return stats;
        }


        #endregion

        #region Utility Methods

        /// <summary>
        /// Initializes the database with data if it's empty
        /// </summary>
        public async Task EnsureDataAsync()
        {
            var hasUsers = await HasUsersAsync();
            var hasFoodData = await HasAnyDataAsync();

            if (!hasUsers && !hasFoodData)
            {
                _logger.LogInformation("Database is empty. Seeding all data...");
                await SeedAllDataAsync();
            }
            else if (!hasUsers)
            {
                _logger.LogInformation("Food data exists but no users found. Seeding users...");
                await SeedUsersAndRolesAsync();
            }
            else if (!hasFoodData)
            {
                _logger.LogInformation("Users exist but no food data found. Seeding food data...");
                await SeedFoodDataAsync();
            }
            else
            {
                _logger.LogInformation("Database already contains data. Skipping seeding.");
                await LogDataStatisticsAsync();
                await LogUserStatisticsAsync();
            }
        }

        /// <summary>
        /// Prints current user statistics to the logger
        /// </summary>
        public async Task LogUserStatisticsAsync()
        {
            var stats = await GetUserStatisticsAsync();
            _logger.LogInformation(stats.ToString());
        }

        /// <summary>
        /// Performs a health check on the data
        /// </summary>
        public async Task<HealthCheckResult> PerformHealthCheckAsync()
        {
            var result = new HealthCheckResult();

            try
            {
                // Check database connectivity
                result.CanConnectToDatabase = await _context.Database.CanConnectAsync();

                if (result.CanConnectToDatabase)
                {
                    // Get statistics
                    result.Statistics = await GetDataStatisticsAsync();
                    result.HasData = result.Statistics.TotalRecords > 0;
                    result.HasCompleteData = await HasCompleteDataAsync();

                    // Validate JSON files
                    result.JsonValidation = await ValidateJsonDataAsync();

                    result.IsHealthy = result.HasCompleteData && result.JsonValidation.IsValid;
                }

                result.CheckedAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                result.Error = ex.Message;
                result.IsHealthy = false;
                _logger.LogError(ex, "Health check failed");
            }

            return result;
        }

        #endregion

        public class HealthCheckResult
        {
            public bool IsHealthy { get; set; }
            public bool CanConnectToDatabase { get; set; }
            public bool HasData { get; set; }
            public bool HasCompleteData { get; set; }
            public DataCleaner.DataStatistics? Statistics { get; set; }
            public DataValidationHelper.ValidationResult? JsonValidation { get; set; }
            public string? Error { get; set; }
            public DateTime CheckedAt { get; set; }

            public override string ToString()
            {
                var status = IsHealthy ? "✅ HEALTHY" : "❌ UNHEALTHY";
                return $@"
=== Health Check Report ===
Status: {status}
Database Connected: {(CanConnectToDatabase ? "✅" : "❌")}
Has Data: {(HasData ? "✅" : "❌")}
Complete Data: {(HasCompleteData ? "✅" : "❌")}
JSON Valid: {(JsonValidation?.IsValid == true ? "✅" : "❌")}
Checked At: {CheckedAt:yyyy-MM-dd HH:mm:ss}
{(string.IsNullOrEmpty(Error) ? "" : $"Error: {Error}")}

{Statistics?.ToString() ?? "No statistics available"}";
            }
        }

        public class UserStatistics
        {
            public int TotalUsers { get; set; }
            public int AdminUsers { get; set; }
            public int StaffUsers { get; set; }
            public int NutritionistUsers { get; set; }
            public int CustomerUsers { get; set; }
            public int TotalRoles { get; set; }

            public override string ToString()
            {
                return $"User Statistics - Total: {TotalUsers} | Admin: {AdminUsers} | Staff: {StaffUsers} | Nutritionist: {NutritionistUsers} | Customer: {CustomerUsers} | Roles: {TotalRoles}";
            }
        }
    }
} 