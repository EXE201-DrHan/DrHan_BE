using DrHan.Domain.Entities.Ingredients;
using DrHan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DrHan.Infrastructure.Services
{
    public interface IFoodRecognitionService
    {
        Task<List<DetectedFood>> AnalyzeImageAsync(byte[] imageData);
        Task<List<DetectedFood>> AnalyzeImageUrlAsync(string imageUrl);
        Task<List<DetectedFood>> AnalyzeImageWithIngredientMatchingAsync(byte[] imageData);
        Task<List<DetectedFood>> AnalyzeImageUrlWithIngredientMatchingAsync(string imageUrl);
    }
    public class FoodRecognitionService : IFoodRecognitionService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FoodRecognitionService> _logger;
        private readonly ApplicationDbContext _context;

        public FoodRecognitionService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<FoodRecognitionService> logger,
            ApplicationDbContext context)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _context = context;
        }

        public async Task<List<DetectedFood>> AnalyzeImageAsync(byte[] imageData)
        {
            try
            {
                // Use OpenAI Vision API for food recognition
                var detectedFoods = await AnalyzeWithOpenAI(imageData);

                // Try to match with database ingredients
                await MatchWithDatabaseIngredientsAsync(detectedFoods);

                return detectedFoods;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing image with OpenAI");
                // Fallback to mock data if OpenAI fails
                return await MockFoodRecognition(imageData);
            }
        }

        public async Task<List<DetectedFood>> AnalyzeImageUrlAsync(string imageUrl)
        {
            try
            {
                var detectedFoods = await AnalyzeWithOpenAIUrl(imageUrl);

                // Try to match with database ingredients
                await MatchWithDatabaseIngredientsAsync(detectedFoods);

                return detectedFoods;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading and analyzing image from URL: {Url}", imageUrl);
                return new List<DetectedFood>();
            }
        }

        public async Task<List<DetectedFood>> AnalyzeImageWithIngredientMatchingAsync(byte[] imageData)
        {
            // This method explicitly focuses on ingredient matching
            var detectedFoods = await AnalyzeImageAsync(imageData);

            // Enhanced ingredient matching with fuzzy search
            await EnhancedIngredientMatchingAsync(detectedFoods);

            return detectedFoods;
        }

        public async Task<List<DetectedFood>> AnalyzeImageUrlWithIngredientMatchingAsync(string imageUrl)
        {
            var detectedFoods = await AnalyzeImageUrlAsync(imageUrl);

            // Enhanced ingredient matching with fuzzy search
            await EnhancedIngredientMatchingAsync(detectedFoods);

            return detectedFoods;
        }

        private async Task<List<DetectedFood>> AnalyzeWithOpenAI(byte[] imageData)
        {
            var apiKey = _configuration["OpenAI:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning("OpenAI API key not configured, using mock data");
                return await MockFoodRecognition(imageData);
            }

            var base64Image = Convert.ToBase64String(imageData);

            var requestBody = new
            {
                model = "gpt-4o",
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new
                            {
                                type = "text",
                                text = @"Phân tích hình ảnh thức ăn này và xác định tất cả các thành phần, nguyên liệu mà bạn có thể nhìn thấy. 
                                        Đặc biệt chú ý đến các món ăn Việt Nam và nguyên liệu địa phương như:
                                        - Nước mắm, tương ớt, mắm tôm
                                        - Bánh phở, bánh mì, bánh cuốn
                                        - Tôm, cua, cá, mực
                                        - Đậu phụ, đậu nành
                                        - Lạc (đậu phộng), hạt điều
                                        - Sữa dừa, dừa nạo
                                        - Trứng gà, trứng vịt
                                        
                                        Trả về kết quả dưới dạng mảng JSON với mỗi item có thuộc tính 'name' (bằng tiếng Việt) và 'confidence' (0.0-1.0). 
                                        Tập trung vào các nguyên liệu riêng lẻ thay vì tên món ăn. Ví dụ: nếu thấy phở, hãy liệt kê 'bánh phở', 'nước dùng xương', 'thịt bò', v.v.
                                        Hãy cụ thể về các nguyên liệu thường chứa chất gây dị ứng.
                                        
                                        Định dạng mẫu:
                                        [
                                          {""name"": ""phô mai"", ""confidence"": 0.95},
                                          {""name"": ""bột mì"", ""confidence"": 0.85},
                                          {""name"": ""cà chua"", ""confidence"": 0.90}
                                        ]"
                            },
                            new
                            {
                                type = "image_url",
                                image_url = new
                                {
                                    url = $"data:image/jpeg;base64,{base64Image}",
                                    detail = "high"
                                }
                            }
                        }
                    }
                },
                max_tokens = 1000,
                temperature = 0.1
            };

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
            var responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("OpenAI API error: {StatusCode} - {Response}", response.StatusCode, responseJson);
                return await MockFoodRecognition(imageData);
            }

            return await ParseOpenAIResponseAsync(responseJson);
        }

        private async Task<List<DetectedFood>> AnalyzeWithOpenAIUrl(string imageUrl)
        {
            var apiKey = _configuration["OpenAI:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning("OpenAI API key not configured");
                return new List<DetectedFood>();
            }

            var requestBody = new
            {
                model = "gpt-4o",
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new
                            {
                                type = "text",
                                text = @"Phân tích hình ảnh thức ăn này và xác định tất cả các thành phần, nguyên liệu mà bạn có thể nhìn thấy. 
                                        Đặc biệt chú ý đến các món ăn Việt Nam và nguyên liệu địa phương như:
                                        - Nước mắm, tương ớt, mắm tôm
                                        - Bánh phở, bánh mì, bánh cuốn
                                        - Tôm, cua, cá, mực
                                        - Đậu phụ, đậu nành
                                        - Lạc (đậu phộng), hạt điều
                                        - Sữa dừa, dừa nạo
                                        - Trứng gà, trứng vịt
                                        
                                        Trả về kết quả dưới dạng mảng JSON với mỗi item có thuộc tính 'name' (bằng tiếng Việt) và 'confidence' (0.0-1.0). 
                                        Tập trung vào các nguyên liệu riêng lẻ thay vì tên món ăn. Ví dụ: nếu thấy phở, hãy liệt kê 'bánh phở', 'nước dùng xương', 'thịt bò', v.v.
                                        Hãy cụ thể về các nguyên liệu thường chứa chất gây dị ứng.
                                        
                                        Định dạng mẫu:
                                        [
                                          {""name"": ""phô mai"", ""confidence"": 0.95},
                                          {""name"": ""bột mì"", ""confidence"": 0.85},
                                          {""name"": ""cà chua"", ""confidence"": 0.90}
                                        ]"
                            },
                            new
                            {
                                type = "image_url",
                                image_url = new
                                {
                                    url = imageUrl,
                                    detail = "high"
                                }
                            }
                        }
                    }
                },
                max_tokens = 1000,
                temperature = 0.1
            };

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
            var responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("OpenAI API error: {StatusCode} - {Response}", response.StatusCode, responseJson);
                return new List<DetectedFood>();
            }

            return await ParseOpenAIResponseAsync(responseJson);
        }

        private async Task<List<DetectedFood>> ParseOpenAIResponseAsync(string responseJson)
        {
            try
            {
                using var document = JsonDocument.Parse(responseJson);
                var messageContent = document.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                if (string.IsNullOrEmpty(messageContent))
                    return new List<DetectedFood>();

                // Extract JSON from the response
                var jsonStart = messageContent.IndexOf('[');
                var jsonEnd = messageContent.LastIndexOf(']');

                if (jsonStart == -1 || jsonEnd == -1)
                {
                    _logger.LogWarning("Could not find JSON array in OpenAI response");
                    return new List<DetectedFood>();
                }

                var jsonContent = messageContent.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var detectedFoods = JsonSerializer.Deserialize<List<DetectedFood>>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<DetectedFood>();

                // Initialize empty collections
                foreach (var food in detectedFoods)
                {
                    food.PotentialAllergens = food.PotentialAllergens ?? new List<string>();
                }

                return detectedFoods;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing OpenAI response: {Response}", responseJson);
                return new List<DetectedFood>();
            }
        }

        private async Task MatchWithDatabaseIngredientsAsync(List<DetectedFood> detectedFoods)
        {
            if (!detectedFoods.Any()) return;

            try
            {
                // Get all ingredients with their names and allergens for better matching
                var allIngredients = await _context.Ingredients
                    .Include(i => i.IngredientNames)
                    .Include(i => i.IngredientAllergens)
                    .ThenInclude(ia => ia.Allergen)
                    .ToListAsync();

                foreach (var detectedFood in detectedFoods)
                {
                    var bestMatch = FindBestIngredientMatch(detectedFood.Name, allIngredients);

                    if (bestMatch.ingredient != null)
                    {
                        detectedFood.MatchedIngredientId = bestMatch.ingredient.Id;
                        detectedFood.MatchedIngredient = bestMatch.ingredient;

                        // Add allergens from the matched ingredient
                        var allergens = bestMatch.ingredient.IngredientAllergens
                            .Select(ia => ia.Allergen.Name.ToLower())
                            .ToList();

                        detectedFood.PotentialAllergens.AddRange(allergens);

                        _logger.LogInformation("Matched '{DetectedFood}' with ingredient '{IngredientName}' (confidence: {Confidence})",
                            detectedFood.Name, bestMatch.ingredient.Name, bestMatch.confidence);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error matching detected foods with database ingredients");
            }
        }

        private async Task EnhancedIngredientMatchingAsync(List<DetectedFood> detectedFoods)
        {
            if (!detectedFoods.Any()) return;

            try
            {
                // First, do basic matching
                await MatchWithDatabaseIngredientsAsync(detectedFoods);

                // Then, try fuzzy matching for unmatched items
                var unmatchedFoods = detectedFoods.Where(f => f.MatchedIngredientId == null).ToList();

                if (unmatchedFoods.Any())
                {
                    await FuzzyMatchIngredientsAsync(unmatchedFoods);
                }

                // Finally, try category-based matching
                var stillUnmatchedFoods = detectedFoods.Where(f => f.MatchedIngredientId == null).ToList();

                if (stillUnmatchedFoods.Any())
                {
                    await CategoryBasedMatchingAsync(stillUnmatchedFoods);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in enhanced ingredient matching");
            }
        }

        private (Ingredient ingredient, double confidence) FindBestIngredientMatch(string detectedFoodName, List<Ingredient> ingredients)
        {
            var searchTerm = detectedFoodName.ToLower().Trim();
            Ingredient bestMatch = null;
            double bestConfidence = 0.0;

            foreach (var ingredient in ingredients)
            {
                // Check main name
                var mainNameConfidence = CalculateStringConfidence(searchTerm, ingredient.Name.ToLower());
                if (mainNameConfidence > bestConfidence)
                {
                    bestMatch = ingredient;
                    bestConfidence = mainNameConfidence;
                }

                // Check alternative names
                foreach (var name in ingredient.IngredientNames)
                {
                    var altNameConfidence = CalculateStringConfidence(searchTerm, name.Name.ToLower());
                    if (altNameConfidence > bestConfidence)
                    {
                        bestMatch = ingredient;
                        bestConfidence = altNameConfidence;
                    }
                }
            }

            // Only return matches with reasonable confidence
            return bestConfidence >= 0.6 ? (bestMatch, bestConfidence) : (null, 0.0);
        }

        private async Task FuzzyMatchIngredientsAsync(List<DetectedFood> unmatchedFoods)
        {
            var ingredients = await _context.Ingredients
                .Include(i => i.IngredientNames)
                .Include(i => i.IngredientAllergens)
                .ThenInclude(ia => ia.Allergen)
                .ToListAsync();

            foreach (var detectedFood in unmatchedFoods)
            {
                var bestMatch = FindFuzzyIngredientMatch(detectedFood.Name, ingredients);

                if (bestMatch.ingredient != null)
                {
                    detectedFood.MatchedIngredientId = bestMatch.ingredient.Id;
                    detectedFood.MatchedIngredient = bestMatch.ingredient;

                    var allergens = bestMatch.ingredient.IngredientAllergens
                        .Select(ia => ia.Allergen.Name.ToLower())
                        .ToList();

                    detectedFood.PotentialAllergens.AddRange(allergens);

                    _logger.LogInformation("Fuzzy matched '{DetectedFood}' with ingredient '{IngredientName}'",
                        detectedFood.Name, bestMatch.ingredient.Name);
                }
            }
        }

        private async Task CategoryBasedMatchingAsync(List<DetectedFood> unmatchedFoods)
        {
            // Define category keywords
            var categoryKeywords = new Dictionary<string, List<string>>
            {
                { "dairy", new List<string> { "sữa", "phô mai", "kem", "bơ", "cheese", "milk", "cream", "butter" } },
                { "grain", new List<string> { "bột", "bánh", "mì", "gạo", "flour", "bread", "rice", "wheat" } },
                { "meat", new List<string> { "thịt", "gà", "heo", "bò", "meat", "chicken", "pork", "beef" } },
                { "seafood", new List<string> { "cá", "tôm", "cua", "fish", "shrimp", "crab", "seafood" } },
                { "vegetable", new List<string> { "rau", "cải", "cà", "vegetable", "lettuce", "tomato" } }
            };

            foreach (var detectedFood in unmatchedFoods)
            {
                foreach (var category in categoryKeywords)
                {
                    if (category.Value.Any(keyword => detectedFood.Name.ToLower().Contains(keyword)))
                    {
                        var categoryIngredients = await _context.Ingredients
                            .Include(i => i.IngredientAllergens)
                            .ThenInclude(ia => ia.Allergen)
                            .Where(i => i.Category.ToLower() == category.Key)
                            .FirstOrDefaultAsync();

                        if (categoryIngredients != null)
                        {
                            detectedFood.MatchedIngredientId = categoryIngredients.Id;
                            detectedFood.MatchedIngredient = categoryIngredients;

                            var allergens = categoryIngredients.IngredientAllergens
                                .Select(ia => ia.Allergen.Name.ToLower())
                                .ToList();

                            detectedFood.PotentialAllergens.AddRange(allergens);

                            _logger.LogInformation("Category matched '{DetectedFood}' with ingredient '{IngredientName}' in category '{Category}'",
                                detectedFood.Name, categoryIngredients.Name, category.Key);
                            break;
                        }
                    }
                }
            }
        }

        private (Ingredient ingredient, double confidence) FindFuzzyIngredientMatch(string detectedFoodName, List<Ingredient> ingredients)
        {
            var searchTerm = detectedFoodName.ToLower().Trim();
            Ingredient bestMatch = null;
            double bestConfidence = 0.0;

            foreach (var ingredient in ingredients)
            {
                // Use Levenshtein distance for fuzzy matching
                var mainNameConfidence = CalculateFuzzyConfidence(searchTerm, ingredient.Name.ToLower());
                if (mainNameConfidence > bestConfidence)
                {
                    bestMatch = ingredient;
                    bestConfidence = mainNameConfidence;
                }

                foreach (var name in ingredient.IngredientNames)
                {
                    var altNameConfidence = CalculateFuzzyConfidence(searchTerm, name.Name.ToLower());
                    if (altNameConfidence > bestConfidence)
                    {
                        bestMatch = ingredient;
                        bestConfidence = altNameConfidence;
                    }
                }
            }

            // Lower threshold for fuzzy matching
            return bestConfidence >= 0.4 ? (bestMatch, bestConfidence) : (null, 0.0);
        }

        private double CalculateStringConfidence(string search, string target)
        {
            if (string.IsNullOrEmpty(search) || string.IsNullOrEmpty(target))
                return 0.0;

            // Exact match
            if (search == target) return 1.0;

            // Contains match
            if (target.Contains(search)) return 0.9;
            if (search.Contains(target)) return 0.8;

            // Word overlap
            var searchWords = search.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var targetWords = target.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var commonWords = searchWords.Intersect(targetWords).Count();
            var totalWords = Math.Max(searchWords.Length, targetWords.Length);

            return totalWords > 0 ? (double)commonWords / totalWords : 0.0;
        }

        private double CalculateFuzzyConfidence(string search, string target)
        {
            if (string.IsNullOrEmpty(search) || string.IsNullOrEmpty(target))
                return 0.0;

            var distance = CalculateLevenshteinDistance(search, target);
            var maxLength = Math.Max(search.Length, target.Length);

            return maxLength > 0 ? 1.0 - (double)distance / maxLength : 0.0;
        }

        private int CalculateLevenshteinDistance(string source, string target)
        {
            if (string.IsNullOrEmpty(source)) return target?.Length ?? 0;
            if (string.IsNullOrEmpty(target)) return source.Length;

            var matrix = new int[source.Length + 1, target.Length + 1];

            for (int i = 0; i <= source.Length; i++)
                matrix[i, 0] = i;

            for (int j = 0; j <= target.Length; j++)
                matrix[0, j] = j;

            for (int i = 1; i <= source.Length; i++)
            {
                for (int j = 1; j <= target.Length; j++)
                {
                    int cost = source[i - 1] == target[j - 1] ? 0 : 1;
                    matrix[i, j] = Math.Min(
                        Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                        matrix[i - 1, j - 1] + cost);
                }
            }

            return matrix[source.Length, target.Length];
        }

        private async Task<List<DetectedFood>> MockFoodRecognition(byte[] imageData)
        {
            // Mock implementation with database ingredients
            var mockIngredients = await _context.Ingredients
                .Include(i => i.IngredientAllergens)
                .ThenInclude(ia => ia.Allergen)
                .Take(5)
                .ToListAsync();

            var detectedFoods = new List<DetectedFood>();
            var random = new Random();

            foreach (var ingredient in mockIngredients)
            {
                var confidence = 0.6 + (random.NextDouble() * 0.4);
                var allergens = ingredient.IngredientAllergens
                    .Select(ia => ia.Allergen.Name.ToLower())
                    .ToList();

                detectedFoods.Add(new DetectedFood
                {
                    Name = ingredient.Name,
                    Confidence = Math.Round(confidence, 2),
                    MatchedIngredientId = ingredient.Id,
                    MatchedIngredient = ingredient,
                    PotentialAllergens = allergens
                });
            }

            return detectedFoods;
        }
    }
}
