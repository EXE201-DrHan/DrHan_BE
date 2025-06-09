using DrHan.Infrastructure.Seeders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DrHan.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")] // Only admins can access data management operations
    public class DataManagementController : ControllerBase
    {
        private readonly DataManagementService _dataManagementService;
        private readonly ILogger<DataManagementController> _logger;

        public DataManagementController(
            DataManagementService dataManagementService,
            ILogger<DataManagementController> logger)
        {
            _dataManagementService = dataManagementService;
            _logger = logger;
        }

        /// <summary>
        /// Seeds all data (allergens, ingredients, and relationships)
        /// </summary>
        [HttpPost("seed/all")]
        public async Task<IActionResult> SeedAllData()
        {
            try
            {
                await _dataManagementService.SeedAllDataAsync();
                return Ok(new { Message = "All data seeded successfully", Timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to seed all data");
                return StatusCode(500, new { Error = "Failed to seed data", Details = ex.Message });
            }
        }

        /// <summary>
        /// Seeds only allergen data
        /// </summary>
        [HttpPost("seed/allergens")]
        public async Task<IActionResult> SeedAllergenData()
        {
            try
            {
                await _dataManagementService.SeedAllergenDataAsync();
                return Ok(new { Message = "Allergen data seeded successfully", Timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to seed allergen data");
                return StatusCode(500, new { Error = "Failed to seed allergen data", Details = ex.Message });
            }
        }

        /// <summary>
        /// Seeds only ingredient data
        /// </summary>
        [HttpPost("seed/ingredients")]
        public async Task<IActionResult> SeedIngredientData()
        {
            try
            {
                await _dataManagementService.SeedIngredientDataAsync();
                return Ok(new { Message = "Ingredient data seeded successfully", Timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to seed ingredient data");
                return StatusCode(500, new { Error = "Failed to seed ingredient data", Details = ex.Message });
            }
        }

        /// <summary>
        /// Seeds users and roles
        /// </summary>
        [HttpPost("seed/users")]
        public async Task<IActionResult> SeedUsersAndRoles()
        {
            try
            {
                await _dataManagementService.SeedUsersAndRolesAsync();
                var userStats = await _dataManagementService.GetUserStatisticsAsync();
                return Ok(new { 
                    Message = "Users and roles seeded successfully", 
                    UserStatistics = userStats,
                    Timestamp = DateTime.UtcNow 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to seed users and roles");
                return StatusCode(500, new { Error = "Failed to seed users and roles", Details = ex.Message });
            }
        }

        /// <summary>
        /// Seeds only food data (allergens and ingredients, but not users)
        /// </summary>
        [HttpPost("seed/food")]
        public async Task<IActionResult> SeedFoodData()
        {
            try
            {
                await _dataManagementService.SeedFoodDataAsync();
                return Ok(new { Message = "Food data seeded successfully", Timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to seed food data");
                return StatusCode(500, new { Error = "Failed to seed food data", Details = ex.Message });
            }
        }

        /// <summary>
        /// Cleans all seeded data (including users)
        /// </summary>
        [HttpDelete("clean/all")]
        public async Task<IActionResult> CleanAllData()
        {
            try
            {
                await _dataManagementService.CleanAllDataAsync();
                return Ok(new { Message = "All data cleaned successfully", Timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clean all data");
                return StatusCode(500, new { Error = "Failed to clean data", Details = ex.Message });
            }
        }

        /// <summary>
        /// Cleans only food data (allergens and ingredients, but not users)
        /// </summary>
        [HttpDelete("clean/food")]
        public async Task<IActionResult> CleanFoodData()
        {
            try
            {
                await _dataManagementService.CleanFoodDataAsync();
                return Ok(new { Message = "Food data cleaned successfully", Timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clean food data");
                return StatusCode(500, new { Error = "Failed to clean food data", Details = ex.Message });
            }
        }

        /// <summary>
        /// Cleans seeded users and roles (keeps admin)
        /// </summary>
        [HttpDelete("clean/users")]
        public async Task<IActionResult> CleanUsersAndRoles()
        {
            try
            {
                await _dataManagementService.CleanUsersAndRolesAsync();
                return Ok(new { Message = "Users and roles cleaned successfully", Timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clean users and roles");
                return StatusCode(500, new { Error = "Failed to clean users and roles", Details = ex.Message });
            }
        }

        /// <summary>
        /// Resets all data (clean + reseed including users)
        /// </summary>
        [HttpPost("reset/all")]
        public async Task<IActionResult> ResetAllData()
        {
            try
            {
                await _dataManagementService.ResetAllDataAsync();
                return Ok(new { Message = "All data reset successfully", Timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reset all data");
                return StatusCode(500, new { Error = "Failed to reset data", Details = ex.Message });
            }
        }

        /// <summary>
        /// Resets food data only (allergens and ingredients, but not users)
        /// </summary>
        [HttpPost("reset/food")]
        public async Task<IActionResult> ResetFoodData()
        {
            try
            {
                await _dataManagementService.ResetFoodDataAsync();
                return Ok(new { Message = "Food data reset successfully", Timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reset food data");
                return StatusCode(500, new { Error = "Failed to reset food data", Details = ex.Message });
            }
        }

        /// <summary>
        /// Resets users and roles only
        /// </summary>
        [HttpPost("reset/users")]
        public async Task<IActionResult> ResetUsersAndRoles()
        {
            try
            {
                await _dataManagementService.ResetUsersAndRolesAsync();
                var userStats = await _dataManagementService.GetUserStatisticsAsync();
                return Ok(new { 
                    Message = "Users and roles reset successfully",
                    UserStatistics = userStats,
                    Timestamp = DateTime.UtcNow 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reset users and roles");
                return StatusCode(500, new { Error = "Failed to reset users and roles", Details = ex.Message });
            }
        }

        /// <summary>
        /// Gets current database statistics
        /// </summary>
        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatistics()
        {
            try
            {
                var stats = await _dataManagementService.GetDataStatisticsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get statistics");
                return StatusCode(500, new { Error = "Failed to get statistics", Details = ex.Message });
            }
        }

        /// <summary>
        /// Gets current user statistics
        /// </summary>
        [HttpGet("statistics/users")]
        public async Task<IActionResult> GetUserStatistics()
        {
            try
            {
                var userStats = await _dataManagementService.GetUserStatisticsAsync();
                return Ok(userStats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get user statistics");
                return StatusCode(500, new { Error = "Failed to get user statistics", Details = ex.Message });
            }
        }

        /// <summary>
        /// Performs a health check on the data system
        /// </summary>
        [HttpGet("health")]
        public async Task<IActionResult> HealthCheck()
        {
            try
            {
                var health = await _dataManagementService.PerformHealthCheckAsync();
                return Ok(health);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed");
                return StatusCode(500, new { Error = "Health check failed", Details = ex.Message });
            }
        }

        /// <summary>
        /// Validates JSON data files
        /// </summary>
        [HttpGet("validate")]
        public async Task<IActionResult> ValidateJsonData()
        {
            try
            {
                var validation = await _dataManagementService.ValidateJsonDataAsync();
                return Ok(validation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "JSON validation failed");
                return StatusCode(500, new { Error = "JSON validation failed", Details = ex.Message });
            }
        }

        /// <summary>
        /// Ensures database has data (seeds if empty)
        /// </summary>
        [HttpPost("ensure")]
        public async Task<IActionResult> EnsureData()
        {
            try
            {
                await _dataManagementService.EnsureDataAsync();
                var stats = await _dataManagementService.GetDataStatisticsAsync();
                return Ok(new { 
                    Message = "Data ensured successfully", 
                    Statistics = stats,
                    Timestamp = DateTime.UtcNow 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to ensure data");
                return StatusCode(500, new { Error = "Failed to ensure data", Details = ex.Message });
            }
        }
    }
} 