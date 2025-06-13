using DrHan.Application.Commons;
using DrHan.Application.DTOs.Gemini;
using DrHan.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DrHan.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize(Roles = "Admin")] // Uncomment to restrict to admin users
    public class RecipeCacheController : ControllerBase
    {
        private readonly IRecipeCacheService _recipeCacheService;
        private readonly IGeminiRecipeService _geminiRecipeService;
        private readonly ILogger<RecipeCacheController> _logger;

        public RecipeCacheController(
            IRecipeCacheService recipeCacheService,
            IGeminiRecipeService geminiRecipeService,
            ILogger<RecipeCacheController> logger)
        {
            _recipeCacheService = recipeCacheService ?? throw new ArgumentNullException(nameof(recipeCacheService));
            _geminiRecipeService = geminiRecipeService ?? throw new ArgumentNullException(nameof(geminiRecipeService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Manually trigger recipe pre-population from Gemini API
        /// </summary>
        /// <returns>Number of recipes added to the cache</returns>
        [HttpPost("populate")]
        public async Task<ActionResult<AppResponse<RecipeCacheResponse>>> PopulateRecipeCache()
        {
            try
            {
                _logger.LogInformation("Manual recipe cache population triggered");
                
                var recipesAdded = await _recipeCacheService.PrePopulatePopularRecipesAsync();
                
                var response = new AppResponse<RecipeCacheResponse>()
                    .SetSuccessResponse(new RecipeCacheResponse
                    {
                        Message = $"Recipe cache population completed successfully",
                        RecipesAdded = recipesAdded,
                        Timestamp = DateTime.UtcNow
                    });

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to populate recipe cache");
                var errorResponse = new AppResponse<RecipeCacheResponse>()
                    .SetErrorResponse("PopulateOperation", "Failed to populate recipe cache")
                    .SetErrorResponse("Details", ex.Message);
                return StatusCode(500, errorResponse);
            }
        }

        /// <summary>
        /// Test Gemini API directly with a search query
        /// </summary>
        /// <param name="searchQuery">Search query to test</param>
        /// <returns>Raw recipes from Gemini API</returns>
        [HttpPost("test-gemini")]
        public async Task<ActionResult<AppResponse<List<GeminiRecipeResponseDto>>>> TestGeminiApi([FromQuery] string searchQuery = "chicken recipes")
        {
            try
            {
                _logger.LogInformation("Testing Gemini API with query: {SearchQuery}", searchQuery);
                
                var request = new GeminiRecipeRequestDto
                {
                    SearchQuery = searchQuery,
                    Count = 2
                };

                var recipes = await _geminiRecipeService.SearchRecipesAsync(request);
                
                var response = new AppResponse<List<GeminiRecipeResponseDto>>()
                    .SetSuccessResponse(recipes);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to test Gemini API");
                var errorResponse = new AppResponse<List<GeminiRecipeResponseDto>>()
                    .SetErrorResponse("TestOperation", "Failed to test Gemini API")
                    .SetErrorResponse("Details", ex.Message);
                return StatusCode(500, errorResponse);
            }
        }

        /// <summary>
        /// Get information about the recipe cache service status
        /// </summary>
        [HttpGet("status")]
        public ActionResult<AppResponse<RecipeCacheStatusResponse>> GetCacheStatus()
        {
            try
            {
                var response = new AppResponse<RecipeCacheStatusResponse>()
                    .SetSuccessResponse(new RecipeCacheStatusResponse
                    {
                        Message = "Recipe cache service is available for manual population",
                        IsBackgroundServiceEnabled = false, // This would need to be read from config if needed
                        LastPopulatedAt = null, // Could be tracked in database if needed
                        Timestamp = DateTime.UtcNow
                    });

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get cache status");
                var errorResponse = new AppResponse<RecipeCacheStatusResponse>()
                    .SetErrorResponse("StatusOperation", "Failed to get cache status")
                    .SetErrorResponse("Details", ex.Message);
                return StatusCode(500, errorResponse);
            }
        }
    }

    public class RecipeCacheResponse
    {
        public string Message { get; set; } = string.Empty;
        public int RecipesAdded { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class RecipeCacheStatusResponse
    {
        public string Message { get; set; } = string.Empty;
        public bool IsBackgroundServiceEnabled { get; set; }
        public DateTime? LastPopulatedAt { get; set; }
        public DateTime Timestamp { get; set; }
    }
} 