using DrHan.Application.DTOs.Gemini;
using DrHan.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DrHan.Infrastructure.ExternalServices;

public class GeminiRecipeService : IGeminiRecipeService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GeminiRecipeService> _logger;

    private const string GeminiApiBaseUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";
    private const int MaxRetryAttempts = 3;
    private const int BaseDelayMs = 1000;

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
        if (request == null)
        {
            _logger.LogWarning("Request object is null");
            return new List<GeminiRecipeResponseDto>();
        }

        if (string.IsNullOrWhiteSpace(request.SearchQuery))
        {
            _logger.LogWarning("Search query is empty or null");
            return new List<GeminiRecipeResponseDto>();
        }

        try
        {
            var apiKey = GetApiKey();
            if (apiKey == null)
            {
                return new List<GeminiRecipeResponseDto>();
            }

            var recipes = await ExecuteWithRetry(async () =>
                await CallGeminiApiAsync(request, apiKey));

            _logger.LogInformation("Successfully retrieved {Count} recipes for query: {Query}",
                recipes.Count, request.SearchQuery);

            return recipes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while searching recipes for query: {Query}",
                request.SearchQuery);
            return new List<GeminiRecipeResponseDto>();
        }
    }

    private string? GetApiKey()
    {
        var apiKey = _configuration["Gemini:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogError("Gemini API key is not configured. Please set the 'Gemini:ApiKey' configuration value.");
            return null;
        }
        return apiKey;
    }

    private async Task<List<GeminiRecipeResponseDto>> ExecuteWithRetry(
        Func<Task<List<GeminiRecipeResponseDto>>> operation)
    {
        var lastException = new Exception();

        for (int attempt = 1; attempt <= MaxRetryAttempts; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (HttpRequestException ex) when (IsRetryableError(ex))
            {
                lastException = ex;

                if (attempt == MaxRetryAttempts)
                {
                    _logger.LogError(ex, "Final retry attempt failed after {Attempts} attempts", MaxRetryAttempts);
                    break;
                }

                var delay = TimeSpan.FromMilliseconds(BaseDelayMs * Math.Pow(2, attempt - 1));
                _logger.LogWarning("Attempt {Attempt} failed, retrying in {Delay}ms. Error: {Error}",
                    attempt, delay.TotalMilliseconds, ex.Message);

                await Task.Delay(delay);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Non-retryable error occurred on attempt {Attempt}", attempt);
                throw;
            }
        }

        _logger.LogError(lastException, "All retry attempts failed");
        return new List<GeminiRecipeResponseDto>();
    }

    private static bool IsRetryableError(HttpRequestException ex)
    {
        return ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
               ex.Message.Contains("network", StringComparison.OrdinalIgnoreCase) ||
               ex.Message.Contains("502") || // Bad Gateway
               ex.Message.Contains("503") || // Service Unavailable
               ex.Message.Contains("504");   // Gateway Timeout
    }

    private async Task<List<GeminiRecipeResponseDto>> CallGeminiApiAsync(
        GeminiRecipeRequestDto request,
        string apiKey)
    {
        var prompt = BuildPrompt(request);
        var requestBody = CreateApiRequestBody(prompt);

        using var httpContent = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json");

        var requestUri = $"{GeminiApiBaseUrl}?key={apiKey}";

        _logger.LogDebug("Sending request to Gemini API for query: {Query}", request.SearchQuery);

        using var response = await _httpClient.PostAsync(requestUri, httpContent);

        if (!response.IsSuccessStatusCode)
        {
            await HandleApiError(response);
            return new List<GeminiRecipeResponseDto>();
        }

        var responseBytes = await response.Content.ReadAsByteArrayAsync();
        var responseContent = System.Text.Encoding.UTF8.GetString(responseBytes);

        return await ParseGeminiResponse(responseContent);
    }

    private static object CreateApiRequestBody(string prompt)
    {
        return new
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
            },
            generationConfig = new
            {
                temperature = 0.1,
                maxOutputTokens = 4000,
                topP = 0.8,
                topK = 40
            }
        };
    }

    private async Task HandleApiError(HttpResponseMessage response)
    {
        var errorContent = await response.Content.ReadAsStringAsync();

        var logLevel = response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => LogLevel.Error,
            HttpStatusCode.Forbidden => LogLevel.Error,
            HttpStatusCode.TooManyRequests => LogLevel.Warning,
            HttpStatusCode.BadRequest => LogLevel.Warning,
            _ => LogLevel.Error
        };

        _logger.Log(logLevel,
            "Gemini API request failed with status {StatusCode}. Response: {Response}",
            response.StatusCode,
            errorContent);

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            throw new HttpRequestException($"Rate limited by Gemini API: {response.StatusCode}");
        }

        if (response.StatusCode >= HttpStatusCode.InternalServerError)
        {
            throw new HttpRequestException($"Gemini API server error: {response.StatusCode}");
        }
    }

    private string BuildPrompt(GeminiRecipeRequestDto request)
    {
        var promptBuilder = new StringBuilder();

        promptBuilder.AppendLine($"Vui lòng cung cấp {Math.Max(1, Math.Min(request.Count, 10))} công thức nấu ăn chi tiết dựa trên các tiêu chí sau:");
        promptBuilder.AppendLine($"- Từ khóa tìm kiếm: {request.SearchQuery}");

        AppendOptionalCriteria(promptBuilder, "Loại ẩm thực", request.CuisineType);
        AppendOptionalCriteria(promptBuilder, "Loại bữa ăn", request.MealType);
        AppendOptionalCriteria(promptBuilder, "Mức độ khó", request.DifficultyLevel);

        if (request.MaxPrepTime.HasValue)
            promptBuilder.AppendLine($"- Thời gian chuẩn bị tối đa: {request.MaxPrepTime} phút");

        if (request.MaxCookTime.HasValue)
            promptBuilder.AppendLine($"- Thời gian nấu tối đa: {request.MaxCookTime} phút");

        if (request.Servings.HasValue)
            promptBuilder.AppendLine($"- Số khẩu phần: {request.Servings}");

        if (request.ExcludeAllergens?.Any() == true)
            promptBuilder.AppendLine($"- Loại trừ chất gây dị ứng: {string.Join(", ", request.ExcludeAllergens)}");

        promptBuilder.AppendLine();
        promptBuilder.AppendLine("QUAN TRỌNG: Trả lời chỉ bằng một mảng JSON hợp lệ. Không sử dụng comment, không có text giải thích.");
        promptBuilder.AppendLine("Không sử dụng ký tự '/' trong bất kỳ giá trị string nào trừ khi được escape đúng cách.");
        promptBuilder.AppendLine("Tất cả strings phải được wrap trong dấu ngoặc kép và escape các ký tự đặc biệt.");
        promptBuilder.AppendLine("Đảm bảo JSON format hoàn toàn hợp lệ theo chuẩn RFC 7159.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine(GetJsonTemplate());

        return promptBuilder.ToString();
    }

    private static void AppendOptionalCriteria(StringBuilder builder, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            builder.AppendLine($"- {label}: {value}");
        }
    }

    private static string GetJsonTemplate()
    {
        return @"[
  {
    ""name"": ""Tên món ăn"",
    ""description"": ""Mô tả ngắn gọn"",
    ""cuisineType"": ""Việt Nam"",
    ""mealType"": ""Trưa"",
    ""prepTimeMinutes"": 15,
    ""cookTimeMinutes"": 30,
    ""servings"": 4,
    ""difficultyLevel"": ""Dễ"",
    ""ingredients"": [
      {
        ""name"": ""Tên nguyên liệu"",
        ""quantity"": 1.5,
        ""unit"": ""chén"",
        ""notes"": ""ghi chú""
      }
    ],
    ""instructions"": [
      {
        ""stepNumber"": 1,
        ""instruction"": ""Mô tả bước thực hiện"",
        ""estimatedTimeMinutes"": 5
      }
    ],
    ""allergens"": [""Gluten""],
    ""allergenFreeClaims"": [""Không chứa sữa""]
  }
]";
    }

    private async Task<List<GeminiRecipeResponseDto>> ParseGeminiResponse(string responseContent)
    {
        try
        {
            var geminiResponse = JsonSerializer.Deserialize<GeminiApiResponse>(responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var content = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
            if (string.IsNullOrWhiteSpace(content))
            {
                _logger.LogWarning("Received empty content from Gemini API");
                return new List<GeminiRecipeResponseDto>();
            }
            _logger.LogInformation(content);
            return await ExtractAndParseJsonFromContent(content);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize Gemini API response");
            return new List<GeminiRecipeResponseDto>();
        }
    }

    private async Task<List<GeminiRecipeResponseDto>> ExtractAndParseJsonFromContent(string content)
    {
        try
        {
            // Enhanced JSON extraction and cleaning
            var cleanedJson = ExtractAndCleanJson(content);
            if (string.IsNullOrWhiteSpace(cleanedJson))
            {
                _logger.LogWarning("Could not extract valid JSON from Gemini response");
                return new List<GeminiRecipeResponseDto>();
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            var recipes = JsonSerializer.Deserialize<List<GeminiRecipeResponseDto>>(cleanedJson, options);

            if (recipes == null || !recipes.Any())
            {
                _logger.LogWarning("No recipes found in parsed JSON response");
                return new List<GeminiRecipeResponseDto>();
            }

            var validRecipes = recipes.Where(IsValidRecipe).ToList();

            _logger.LogInformation("Successfully parsed {ValidCount} valid recipes out of {TotalCount} from Gemini API",
                validRecipes.Count, recipes.Count);

            return validRecipes;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse JSON content from Gemini response. Content preview: {ContentPreview}",
                content.Length > 500 ? content[..500] + "..." : content);

            return await TryAlternativeJsonParsing(content);
        }
    }

    private string ExtractAndCleanJson(string content)
    {
        try
        {
            // First, try to find JSON array or object
            var jsonMatch = Regex.Match(content, @"\[[\s\S]*\]|\{[\s\S]*\}");
            if (!jsonMatch.Success)
            {
                _logger.LogWarning("No JSON array or object found in content");
                return string.Empty;
            }

            var json = jsonMatch.Value;

            // Clean up common issues
            json = CleanJsonContentAdvanced(json);

            // Validate JSON structure
            if (!IsValidJsonStructure(json))
            {
                _logger.LogWarning("JSON structure validation failed after cleaning");
                return string.Empty;
            }

            return json;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting and cleaning JSON");
            return string.Empty;
        }
    }

    private string CleanJsonContentAdvanced(string json)
    {
        try
        {
            // Replace escaped quotes that might be causing issues
            json = json.Replace("\\\"", "\"");

            // Fix fraction values (e.g., "1/2" or "1\/2" -> "0.5")
            json = Regex.Replace(json, @"(\d+)\\/?(\d+)", match =>
            {
                var numerator = decimal.Parse(match.Groups[1].Value);
                var denominator = decimal.Parse(match.Groups[2].Value);
                return (numerator / denominator).ToString(System.Globalization.CultureInfo.InvariantCulture);
            });

            // Create a case-insensitive dictionary to store unique mappings
            var uniqueMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Define all possible Vietnamese character mappings
            var allMappings = new[]
            {
                // Common words
                ("Th?t", "Thịt"),
                ("th?t", "thịt"),
                ("Heo", "Heo"),
                ("Kho", "Kho"),
                ("Tiêu", "Tiêu"),
                ("tiêu", "tiêu"),
                ("cho", "cho"),
                ("ngu?i", "người"),
                ("Món", "Món"),
                ("món", "món"),
                ("d?m dà", "đậm đà"),
                ("thom ngon", "thơm ngon"),
                ("thích h?p", "thích hợp"),
                ("b?a", "bữa"),
                ("com", "cơm"),
                ("m?t mình", "một mình"),
                ("Vi?t Nam", "Việt Nam"),
                ("Trua", "Trưa"),
                ("D?", "Dễ"),

                // Ingredients
                ("Th?t ba ch?", "Thịt ba chỉ"),
                ("gram", "gram"),
                ("Ch?n", "Chọn"),
                ("ch?n", "chọn"),
                ("có", "có"),
                ("c? n?c", "có nạc"),
                ("m?", "mỡ"),
                ("Nu?c m?m", "Nước mắm"),
                ("nu?c m?m", "nước mắm"),
                ("mu?ng canh", "muỗng canh"),
                ("Ch?n lo?i", "Chọn loại"),
                ("ch?n lo?i", "chọn loại"),
                ("ngon", "ngon"),
                ("Du?ng", "Đường"),
                ("du?ng", "đường"),
                ("mu?ng cà phê", "muỗng cà phê"),
                ("Có th?", "Có thể"),
                ("có th?", "có thể"),
                ("dùng", "dùng"),
                ("th?t n?t", "thật nết"),
                ("Tiêu xanh", "Tiêu xanh"),
                ("tiêu xanh", "tiêu xanh"),
                ("Gia d?p", "Giã dập"),
                ("gia d?p", "giã dập"),
                ("Hành tím", "Hành tím"),
                ("hành tím", "hành tím"),
                ("Bam nh?", "Băm nhỏ"),
                ("bam nh?", "băm nhỏ"),
                ("T?i", "Tỏi"),
                ("t?i", "tỏi"),
                ("tép", "tép"),
                ("?t", "Ớt"),
                ("tùy ch?n", "tùy chọn"),
                ("Thái lát", "Thái lát"),
                ("thái lát", "thái lát"),
                ("D?u an", "Dầu ăn"),
                ("d?u an", "dầu ăn"),
                ("Nu?c l?c", "Nước lọc"),
                ("nu?c l?c", "nước lọc"),
                ("ml", "ml"),

                // Cooking instructions
                ("r?a s?ch", "rửa sạch"),
                ("thái mi?ng", "thái miếng"),
                ("v?a an", "vừa ăn"),
                ("U?p", "Ướp"),
                ("u?p", "ướp"),
                ("t?i bam", "tỏi băm"),
                ("n?u dùng", "nếu dùng"),
                ("kho?ng", "khoảng"),
                ("phút", "phút"),
                ("Cho", "Cho"),
                ("cho", "cho"),
                ("vào", "vào"),
                ("n?i", "nồi"),
                ("dun nóng", "đun nóng"),
                ("da u?p", "đã ướp"),
                ("xào san", "xào săn"),
                ("l?i", "lại"),
                ("Thêm", "Thêm"),
                ("thêm", "thêm"),
                ("dun sôi", "đun sôi"),
                ("H? nh? l?a", "Hạ nhỏ lửa"),
                ("h? nh? l?a", "hạ nhỏ lửa"),
                ("kho liu riu", "kho liu riu"),
                ("cho d?n khi", "cho đến khi"),
                ("th?t m?m", "thịt mềm"),
                ("nu?c kho", "nước kho"),
                ("sánh l?i", "sánh lại"),
                ("Nêm n?m", "Nêm nếm"),
                ("nêm n?m", "nêm nếm"),
                ("gia v?", "gia vị"),
                ("T?t b?p", "Tắt bếp"),
                ("t?t b?p", "tắt bếp")
            };

            // Add mappings to dictionary, automatically handling duplicates
            foreach (var (key, value) in allMappings)
            {
                if (!uniqueMappings.ContainsKey(key))
                {
                    uniqueMappings[key] = value;
                }
            }

            // Process replacements in order of key length (longest first) to avoid partial matches
            foreach (var pair in uniqueMappings.OrderByDescending(x => x.Key.Length))
            {
                json = json.Replace(pair.Key, pair.Value);
            }

            return json;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning JSON content");
            return json;
        }
    }

    private bool IsValidJsonStructure(string json)
    {
        try
        {
            // Try to parse the JSON to validate its structure
            JsonDocument.Parse(json);
                return true;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning("JSON structure validation failed: {Error}", ex.Message);
            return false;
        }
    }

    private async Task<List<GeminiRecipeResponseDto>> TryAlternativeJsonParsing(string content)
    {
        try
        {
            _logger.LogInformation("Attempting alternative JSON parsing approach");

            // More aggressive cleaning
            var cleanedContent = content
                .Replace("```json", "")
                .Replace("```", "")
                .Trim();

            // Remove all comment patterns more aggressively
            cleanedContent = Regex.Replace(cleanedContent, @"//.*$", "", RegexOptions.Multiline);
            cleanedContent = Regex.Replace(cleanedContent, @"/\*.*?\*/", "", RegexOptions.Singleline);

            var recipes = new List<GeminiRecipeResponseDto>();
            var jsonObjects = ExtractJsonObjects(cleanedContent);

            foreach (var jsonObject in jsonObjects)
            {
                try
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        AllowTrailingCommas = true,
                        ReadCommentHandling = JsonCommentHandling.Skip,
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    };

                    var recipe = JsonSerializer.Deserialize<GeminiRecipeResponseDto>(jsonObject, options);
                    if (recipe != null && IsValidRecipe(recipe))
                    {
                        recipes.Add(recipe);
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse individual recipe object: {ObjectPreview}",
                        jsonObject.Length > 100 ? jsonObject[..100] + "..." : jsonObject);
                }
            }

            _logger.LogInformation("Alternative parsing extracted {Count} valid recipes", recipes.Count);
            return recipes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Alternative JSON parsing also failed");
            return new List<GeminiRecipeResponseDto>();
        }
    }

    private List<string> ExtractJsonObjects(string content)
    {
        var objects = new List<string>();
        var bracketCount = 0;
        var currentObject = new StringBuilder();
        var inString = false;
        var escapeNext = false;

        foreach (char c in content)
        {
            if (escapeNext)
            {
                currentObject.Append(c);
                escapeNext = false;
                continue;
            }

            if (c == '\\')
            {
                currentObject.Append(c);
                escapeNext = true;
                continue;
            }

            if (c == '"' && !escapeNext)
            {
                inString = !inString;
            }

            if (!inString)
            {
                if (c == '{')
                {
                    bracketCount++;
                }
                else if (c == '}')
                {
                    bracketCount--;
                }
            }

            currentObject.Append(c);

            if (!inString && bracketCount == 0 && currentObject.Length > 1 && currentObject[0] == '{')
            {
                objects.Add(currentObject.ToString().Trim());
                currentObject.Clear();
            }
        }

        return objects;
    }

    private static bool IsValidRecipe(GeminiRecipeResponseDto recipe)
    {
        return !string.IsNullOrWhiteSpace(recipe?.Name) &&
               !string.IsNullOrWhiteSpace(recipe?.Description) &&
               recipe.Ingredients?.Any() == true &&
               recipe.Instructions?.Any() == true;
    }
}