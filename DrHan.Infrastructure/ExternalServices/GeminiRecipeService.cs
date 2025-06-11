using DrHan.Application.DTOs.Gemini;
using DrHan.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace DrHan.Infrastructure.ExternalServices;

public class GeminiRecipeService : IGeminiRecipeService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GeminiRecipeService> _logger;

    public GeminiRecipeService(
        HttpClient httpClient, 
        IConfiguration configuration,
        ILogger<GeminiRecipeService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<GeminiRecipeResponseDto>> SearchRecipesAsync(GeminiRecipeRequestDto request)
    {
        try
        {
            var apiKey = _configuration["Gemini:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning("Gemini API key is not configured");
                return new List<GeminiRecipeResponseDto>();
            }

            var prompt = BuildPrompt(request);

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                $"https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent?key={apiKey}",
                content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Gemini API request failed with status: {StatusCode}. Content: {Content}",
                    response.StatusCode, await response.Content.ReadAsStringAsync());
                return new List<GeminiRecipeResponseDto>();
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var geminiResponse = JsonSerializer.Deserialize<GeminiApiResponse>(responseContent);

            return ParseRecipesFromResponse(geminiResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Gemini API for recipe search");
            return new List<GeminiRecipeResponseDto>();
        }
    }

    private string BuildPrompt(GeminiRecipeRequestDto request)
    {
        var prompt = $@"
Please provide {request.Count} detailed recipes based on the following criteria:
- Search query: {request.SearchQuery}
{(string.IsNullOrEmpty(request.CuisineType) ? "" : $"- Cuisine type: {request.CuisineType}")}
{(string.IsNullOrEmpty(request.MealType) ? "" : $"- Meal type: {request.MealType}")}
{(string.IsNullOrEmpty(request.DifficultyLevel) ? "" : $"- Difficulty level: {request.DifficultyLevel}")}
{(request.MaxPrepTime.HasValue ? $"- Maximum prep time: {request.MaxPrepTime} minutes" : "")}
{(request.MaxCookTime.HasValue ? $"- Maximum cook time: {request.MaxCookTime} minutes" : "")}
{(request.Servings.HasValue ? $"- Servings: {request.Servings}" : "")}
{(request.ExcludeAllergens?.Any() == true ? $"- Exclude allergens: {string.Join(", ", request.ExcludeAllergens)}" : "")}

Please respond with ONLY a valid JSON array in the following format:
[
  {{
    ""name"": ""Recipe Name"",
    ""description"": ""Brief description"",
    ""cuisineType"": ""Italian/Chinese/etc"",
    ""mealType"": ""Breakfast/Lunch/Dinner/Snack"",
    ""prepTimeMinutes"": 15,
    ""cookTimeMinutes"": 30,
    ""servings"": 4,
    ""difficultyLevel"": ""Easy/Medium/Hard"",
    ""ingredients"": [
      {{
        ""name"": ""Ingredient name"",
        ""quantity"": 1.5,
        ""unit"": ""cup/tsp/oz/etc"",
        ""notes"": ""optional notes""
      }}
    ],
    ""instructions"": [
      {{
        ""stepNumber"": 1,
        ""instruction"": ""Step description"",
        ""estimatedTimeMinutes"": 5
      }}
    ],
    ""nutrition"": [
      {{
        ""nutrientName"": ""Calories"",
        ""amount"": 250,
        ""unit"": ""kcal"",
        ""dailyValuePercentage"": 12.5
      }}
    ],
    ""allergens"": [""Dairy"", ""Gluten""],
    ""allergenFreeClaims"": [""Gluten-Free"", ""Dairy-Free""]
  }}
]

Do not include any text before or after the JSON array.";

        return prompt;
    }

    private List<GeminiRecipeResponseDto> ParseRecipesFromResponse(GeminiApiResponse? response)
    {
        try
        {
            var content = response?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
            if (string.IsNullOrEmpty(content))
            {
                _logger.LogWarning("Empty content received from Gemini API");
                return new List<GeminiRecipeResponseDto>();
            }

            // Clean the response to extract JSON
            var jsonStart = content.IndexOf('[');
            var jsonEnd = content.LastIndexOf(']');
            
            if (jsonStart == -1 || jsonEnd == -1)
            {
                _logger.LogWarning("No JSON array found in Gemini response");
                return new List<GeminiRecipeResponseDto>();
            }

            var jsonContent = content.Substring(jsonStart, jsonEnd - jsonStart + 1);
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var recipes = JsonSerializer.Deserialize<List<GeminiRecipeResponseDto>>(jsonContent, options);
            
            _logger.LogInformation("Successfully parsed {Count} recipes from Gemini API", recipes?.Count ?? 0);
            
            return recipes ?? new List<GeminiRecipeResponseDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing Gemini API response");
            return new List<GeminiRecipeResponseDto>();
        }
    }
} 