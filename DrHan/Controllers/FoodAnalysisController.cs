using DrHan.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DrHan.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FoodAnalysisController : ControllerBase
    {
        private readonly IFoodRecognitionService _foodRecognitionService;
        private readonly IVisionService _allergenService;
        private readonly ILogger<FoodAnalysisController> _logger;
        private readonly IMappingService _mappingService;

        public FoodAnalysisController(
            IFoodRecognitionService foodRecognitionService,
            IVisionService allergenService,
            ILogger<FoodAnalysisController> logger,
            IMappingService mappingService)
        {
            _foodRecognitionService = foodRecognitionService;
            _allergenService = allergenService;
            _logger = logger;
            _mappingService = mappingService;
        }

        [HttpPost("analyze")]
        public async Task<ActionResult<FoodAnalysisResponseDto>> AnalyzeFood([FromForm] FoodAnalysisRequest request)
        {
            try
            {
                List<DetectedFood> detectedFoods;

                if (request.Image != null)
                {
                    using var memoryStream = new MemoryStream();
                    await request.Image.CopyToAsync(memoryStream);
                    var imageData = memoryStream.ToArray();

                    detectedFoods = await _foodRecognitionService.AnalyzeImageAsync(imageData);
                }
                else if (!string.IsNullOrEmpty(request.ImageUrl))
                {
                    detectedFoods = await _foodRecognitionService.AnalyzeImageUrlAsync(request.ImageUrl);
                }
                else
                {
                    return BadRequest(new FoodAnalysisResponseDto
                    {
                        Success = false,
                        Message = "Please provide either an image file or image URL"
                    });
                }

                if (!detectedFoods.Any())
                {
                    return Ok(new FoodAnalysisResponseDto
                    {
                        Success = true,
                        Message = "No food items detected in the image",
                        DetectedFoods = new List<DetectedFoodDto1>(),
                        AllergenWarnings = new List<AllergenWarningDto1>()
                    });
                }

                // Analyze allergens with user context
                var allergenWarnings = await _allergenService.AnalyzeAllergensAsync(
                    detectedFoods, request.KnownAllergies, request.UserId);

                var overallRiskScore = CalculateOverallRiskScore(allergenWarnings);

                // Get user allergy context if userId provided
                UserAllergyContext userContext = null;
                if (request.UserId.HasValue)
                {
                    userContext = await _allergenService.GetUserAllergyContextAsync(request.UserId.Value);
                }

                var response = new FoodAnalysisResponse
                {
                    Success = true,
                    Message = $"Successfully analyzed {detectedFoods.Count} food item(s)",
                    DetectedFoods = detectedFoods,
                    AllergenWarnings = allergenWarnings,
                    OverallRiskScore = overallRiskScore,
                    UserAllergyContext = userContext
                };

                // Convert to DTO to break circular references
                var responseDto = _mappingService.MapToDto(response);

                return Ok(responseDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing food image");
                return StatusCode(500, new FoodAnalysisResponseDto
                {
                    Success = false,
                    Message = "An error occurred while analyzing the image"
                });
            }
        }


        [HttpGet("allergens")]
        public async Task<ActionResult<List<DrHan.Domain.Entities.Allergens.Allergen>>> GetAllergens()
        {
            try
            {
                var allergens = await _allergenService.GetAllAllergensAsync();
                return Ok(allergens);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving allergens");
                return StatusCode(500, "An error occurred while retrieving allergens");
            }
        }

        [HttpGet("user/{userId}/allergies")]
        public async Task<ActionResult<UserAllergyContext>> GetUserAllergies(int userId)
        {
            try
            {
                var userContext = await _allergenService.GetUserAllergyContextAsync(userId);
                return Ok(userContext);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user allergies for user {UserId}", userId);
                return StatusCode(500, "An error occurred while retrieving user allergies");
            }
        }

        private double CalculateOverallRiskScore(List<AllergenWarning> warnings)
        {
            if (!warnings.Any()) return 0.0;

            var riskScores = warnings.Select(w => w.RiskLevel switch
            {
                "Critical" => 1.0,
                "High" => 0.8,
                "Medium" => 0.5,
                "Low" => 0.2,
                _ => 0.0
            });

            return Math.Round(riskScores.Average(), 2);
        }
    }
}
