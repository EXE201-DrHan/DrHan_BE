using DrHan.Infrastructure.Seeders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DrHan.Application.Commons;
using DrHan.Application.DTOs.DataManagement;

namespace DrHan.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize(Roles = "Admin")] 
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
        public async Task<ActionResult<AppResponse<SeedDataResponse>>> SeedAllData()
        {
            try
            {
                await _dataManagementService.SeedAllDataAsync();
                var response = new AppResponse<SeedDataResponse>()
                    .SetSuccessResponse(new SeedDataResponse
                    {
                        Message = "All data seeded successfully",
                        Operation = "SeedAll",
                        RecordsAffected = 0, // TODO: Return actual count from service
                        Timestamp = DateTime.UtcNow
                    });
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to seed all data");
                var errorResponse = new AppResponse<SeedDataResponse>()
                    .SetErrorResponse("SeedOperation", "Failed to seed all data")
                    .SetErrorResponse("Details", ex.Message);
                return StatusCode(500, errorResponse);
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
        public async Task<ActionResult<AppResponse<SeedUsersResponse>>> SeedUsersAndRoles()
        {
            try
            {
                await _dataManagementService.SeedUsersAndRolesAsync();
                var userStats = await _dataManagementService.GetUserStatisticsAsync();
                
                var response = new AppResponse<SeedUsersResponse>()
                    .SetSuccessResponse(new SeedUsersResponse
                    {
                        Message = "Users and roles seeded successfully",
                        UserStatistics = ConvertToUserStatisticsDto(userStats),
                        Timestamp = DateTime.UtcNow
                    });
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to seed users and roles");
                var errorResponse = new AppResponse<SeedUsersResponse>()
                    .SetErrorResponse("SeedOperation", "Failed to seed users and roles")
                    .SetErrorResponse("Details", ex.Message);
                return StatusCode(500, errorResponse);
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
        /// Seeds only subscription plans and their features
        /// </summary>
        [HttpPost("seed/subscriptions")]
        public async Task<IActionResult> SeedSubscriptionPlans()
        {
            try
            {
                await _dataManagementService.SeedSubscriptionPlansAsync();
                return Ok(new { Message = "Subscription plans seeded successfully", Timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to seed subscription plans");
                return StatusCode(500, new { Error = "Failed to seed subscription plans", Details = ex.Message });
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
        /// Cleans all recipe data
        /// </summary>
        [HttpDelete("clean/recipes")]
        public async Task<IActionResult> CleanRecipeData()
        {
            try
            {
                await _dataManagementService.CleanRecipeDataAsync();
                return Ok(new { Message = "Recipe data cleaned successfully", Timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clean recipe data");
                return StatusCode(500, new { Error = "Failed to clean recipe data", Details = ex.Message });
            }
        }

        /// <summary>
        /// Cleans all subscription plan data
        /// </summary>
        [HttpDelete("clean/subscriptions")]
        public async Task<IActionResult> CleanSubscriptionPlans()
        {
            try
            {
                await _dataManagementService.CleanSubscriptionPlansAsync();
                return Ok(new { Message = "Subscription plans cleaned successfully", Timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clean subscription plans");
                return StatusCode(500, new { Error = "Failed to clean subscription plans", Details = ex.Message });
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
        /// Resets recipe data (cleans all recipes)
        /// </summary>
        [HttpPost("reset/recipes")]
        public async Task<IActionResult> ResetRecipeData()
        {
            try
            {
                await _dataManagementService.ResetRecipeDataAsync();
                var stats = await _dataManagementService.GetDataStatisticsAsync();
                return Ok(new { 
                    Message = "Recipe data reset successfully",
                    Statistics = stats,
                    Timestamp = DateTime.UtcNow 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reset recipe data");
                return StatusCode(500, new { Error = "Failed to reset recipe data", Details = ex.Message });
            }
        }

        /// <summary>
        /// Resets subscription plan data (cleans and reseeds subscription plans)
        /// </summary>
        [HttpPost("reset/subscriptions")]
        public async Task<IActionResult> ResetSubscriptionPlans()
        {
            try
            {
                await _dataManagementService.ResetSubscriptionPlansAsync();
                return Ok(new { 
                    Message = "Subscription plans reset successfully",
                    Timestamp = DateTime.UtcNow 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reset subscription plans");
                return StatusCode(500, new { Error = "Failed to reset subscription plans", Details = ex.Message });
            }
        }

        /// <summary>
        /// Gets current database statistics
        /// </summary>
        [HttpGet("statistics")]
        public async Task<ActionResult<AppResponse<StatisticsResponse>>> GetStatistics()
        {
            try
            {
                var stats = await _dataManagementService.GetDataStatisticsAsync();
                var response = new AppResponse<StatisticsResponse>()
                    .SetSuccessResponse(ConvertToStatisticsDto(stats));
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get statistics");
                var errorResponse = new AppResponse<StatisticsResponse>()
                    .SetErrorResponse("Statistics", "Failed to retrieve database statistics")
                    .SetErrorResponse("Details", ex.Message);
                return StatusCode(500, errorResponse);
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
        public async Task<ActionResult<AppResponse<HealthCheckResponse>>> HealthCheck()
        {
            try
            {
                var health = await _dataManagementService.PerformHealthCheckAsync();
                var response = new AppResponse<HealthCheckResponse>()
                    .SetSuccessResponse(ConvertToHealthCheckDto(health));
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed");
                var errorResponse = new AppResponse<HealthCheckResponse>()
                    .SetErrorResponse("HealthCheck", "Health check operation failed")
                    .SetErrorResponse("Details", ex.Message);
                return StatusCode(500, errorResponse);
            }
        }

        /// <summary>
        /// Validates JSON data files
        /// </summary>
        [HttpGet("validate")]
        public async Task<ActionResult<AppResponse<ValidationResponse>>> ValidateJsonData()
        {
            try
            {
                var validation = await _dataManagementService.ValidateJsonDataAsync();
                var response = new AppResponse<ValidationResponse>()
                    .SetSuccessResponse(ConvertToValidationDto(validation));
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "JSON validation failed");
                var errorResponse = new AppResponse<ValidationResponse>()
                    .SetErrorResponse("Validation", "JSON validation operation failed")
                    .SetErrorResponse("Details", ex.Message);
                return StatusCode(500, errorResponse);
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

        #region Helper Methods

        private UserStatistics ConvertToUserStatisticsDto(DrHan.Infrastructure.Seeders.DataManagementService.UserStatistics stats)
        {
            return new UserStatistics
            {
                TotalUsers = stats.TotalUsers,
                AdminUsers = stats.AdminUsers,
                StaffUsers = stats.StaffUsers,
                NutritionistUsers = stats.NutritionistUsers,
                CustomerUsers = stats.CustomerUsers,
                TotalRoles = stats.TotalRoles,
                Timestamp = DateTime.UtcNow
            };
        }

        private StatisticsResponse ConvertToStatisticsDto(DrHan.Infrastructure.Seeders.DataCleaner.DataStatistics stats)
        {
            return new StatisticsResponse
            {
                CrossReactivityGroupsCount = stats.CrossReactivityGroupsCount,
                AllergensCount = stats.AllergensCount,
                AllergenNamesCount = stats.AllergenNamesCount,
                IngredientsCount = stats.IngredientsCount,
                IngredientNamesCount = stats.IngredientNamesCount,
                AllergenIngredientRelationsCount = stats.IngredientAllergensCount,
                RecipesCount = stats.RecipesCount,
                RecipeIngredientsCount = stats.RecipeIngredientsCount,
                RecipeInstructionsCount = stats.RecipeInstructionsCount,
                RecipeAllergensCount = stats.RecipeAllergensCount,
                RecipeAllergenFreeClaimsCount = stats.RecipeAllergenFreeClaimsCount,
                RecipeImagesCount = stats.RecipeImagesCount,
                RecipeNutritionsCount = stats.RecipeNutritionsCount,
                TotalRecords = stats.TotalRecords,
                Timestamp = DateTime.UtcNow
            };
        }

        private HealthCheckResponse ConvertToHealthCheckDto(DrHan.Infrastructure.Seeders.DataManagementService.HealthCheckResult health)
        {
            return new HealthCheckResponse
            {
                IsHealthy = health.IsHealthy,
                CanConnectToDatabase = health.CanConnectToDatabase,
                HasData = health.HasData,
                HasCompleteData = health.HasCompleteData,
                Statistics = health.Statistics != null ? ConvertToStatisticsDto(health.Statistics) : null,
                JsonValidation = health.JsonValidation != null ? ConvertToValidationDto(health.JsonValidation) : null,
                Error = health.Error,
                CheckedAt = health.CheckedAt
            };
        }

        private ValidationResponse ConvertToValidationDto(DrHan.Infrastructure.Seeders.DataValidationHelper.ValidationResult validation)
        {
            return new ValidationResponse
            {
                IsValid = validation.IsValid,
                Errors = validation.ValidationErrors.ToArray(),
                Warnings = Array.Empty<string>(), // Not available in current ValidationResult
                FilesChecked = 7, // Fixed number based on JSON files checked
                ValidFiles = validation.IsValid ? 7 : 0,
                InvalidFiles = validation.IsValid ? 0 : 7,
                Details = new Dictionary<string, string>
                {
                    ["CrossReactivityGroups"] = validation.CrossReactivityGroupsCount.ToString(),
                    ["Allergens"] = validation.AllergensCount.ToString(),
                    ["AllergenNames"] = validation.AllergenNamesCount.ToString(),
                    ["AllergenCrossReactivities"] = validation.AllergenCrossReactivitiesCount.ToString(),
                    ["Ingredients"] = validation.IngredientsCount.ToString(),
                    ["IngredientNames"] = validation.IngredientNamesCount.ToString(),
                    ["IngredientAllergens"] = validation.IngredientAllergensCount.ToString()
                },
                Timestamp = DateTime.UtcNow
            };
        }

        #endregion
    }
} 