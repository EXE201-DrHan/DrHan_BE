using DrHan.Application.DTOs.Chatbot;
using DrHan.Application.Interfaces.Services;
using DrHan.Application.Interfaces.Services.CacheService;
using DrHan.Infrastructure.KnowledgeBase;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace DrHan.Infrastructure.Services;

public class ChatbotService : IChatbotService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ICacheService _cacheService;
    private readonly ILogger<ChatbotService> _logger;

    private const int MaxConversationHistory = 10;
    private const int ConversationCacheDurationHours = 24;

    public ChatbotService(
        HttpClient httpClient,
        IConfiguration configuration,
        ICacheService cacheService,
        ILogger<ChatbotService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ChatbotResponseDto> ProcessMessageAsync(ChatbotRequestDto request)
    {
        try
        {
            _logger.LogInformation("Processing chatbot message: {Message}", request.Message);

            // Phân tích intent của tin nhắn
            var (category, confidence) = await AnalyzeIntentAsync(request.Message, request.Language);
            
            // Tạo conversation ID nếu chưa có
            var conversationId = request.ConversationId ?? Guid.NewGuid().ToString();

            // Lấy lịch sử cuộc trò chuyện
            var history = await GetConversationHistoryAsync(conversationId, MaxConversationHistory);

            // Tạo context cho AI
            var context = BuildContextFromKnowledge(category, request.Message);
            
            // Gọi Gemini API
            var aiResponse = await CallGeminiForChatAsync(request.Message, context, history, request.Language);

            // Lưu tin nhắn vào lịch sử
            await SaveMessageToHistoryAsync(conversationId, "user", request.Message);
            await SaveMessageToHistoryAsync(conversationId, "assistant", aiResponse);

            // Tạo suggested actions
            var suggestedActions = GenerateSuggestedActions(category, request.Message);

            // Tạo response
            var response = new ChatbotResponseDto
            {
                Response = aiResponse,
                ConversationId = conversationId,
                Confidence = confidence,
                Category = category,
                SuggestedActions = suggestedActions,
                RequiresHumanSupport = IsEmergencyCase(request.Message),
                RelatedKeywords = GetRelatedKeywords(category)
            };

            _logger.LogInformation("Successfully processed chatbot message for conversation: {ConversationId}", conversationId);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chatbot message: {Message}", request.Message);
            return new ChatbotResponseDto
            {
                Response = "Xin lỗi, tôi đang gặp vấn đề kỹ thuật. Vui lòng thử lại sau hoặc liên hệ hỗ trợ.",
                ConversationId = request.ConversationId ?? Guid.NewGuid().ToString(),
                Confidence = 0.1,
                RequiresHumanSupport = true
            };
        }
    }

    public async Task<List<ChatMessageDto>> GetConversationHistoryAsync(string conversationId, int limit = 20)
    {
        try
        {
            var cacheKey = $"conversation_history:{conversationId}";
            var history = await _cacheService.GetAsync<List<ChatMessageDto>>(cacheKey);
            
            if (history == null)
            {
                return new List<ChatMessageDto>();
            }

            return history.OrderByDescending(x => x.Timestamp)
                         .Take(limit)
                         .OrderBy(x => x.Timestamp)
                         .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversation history: {ConversationId}", conversationId);
            return new List<ChatMessageDto>();
        }
    }

    public async Task ClearConversationAsync(string conversationId)
    {
        try
        {
            var cacheKey = $"conversation_history:{conversationId}";
            await _cacheService.RemoveAsync(cacheKey);
            _logger.LogInformation("Cleared conversation history: {ConversationId}", conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing conversation: {ConversationId}", conversationId);
        }
    }

    public async Task<List<string>> GetSuggestedQuestionsAsync(string category, string language = "vi")
    {
        try
        {
            if (ChatbotKnowledgeBase.SuggestedQuestions.TryGetValue(category, out var questions))
            {
                return questions.Take(5).ToList();
            }

            return ChatbotKnowledgeBase.SuggestedQuestions["general"].Take(5).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting suggested questions for category: {Category}", category);
            return new List<string>();
        }
    }

    public async Task<(string category, double confidence)> AnalyzeIntentAsync(string message, string language = "vi")
    {
        try
        {
            var messageLower = message.ToLower();
            
            var maxScore = 0.0;
            var bestCategory = "general";

            foreach (var kvp in ChatbotKnowledgeBase.Keywords)
            {
                var score = 0.0;
                foreach (var keyword in kvp.Value)
                {
                    if (messageLower.Contains(keyword.ToLower()))
                    {
                        score += 1.0;
                    }
                }
                
                score = score / kvp.Value.Count;
                
                if (score > maxScore)
                {
                    maxScore = score;
                    bestCategory = kvp.Key;
                }
            }

            var category = bestCategory switch
            {
                "meal_planning" => "mealplan",
                "allergy" => "allergy",
                "recipe" => "mealplan",
                "app_features" => "app_help",
                "subscription" => "app_help",
                "emergency" => "allergy",
                _ => "general"
            };

            var confidence = Math.Max(0.3, maxScore);
            return (category, confidence);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing intent for message: {Message}", message);
            return ("general", 0.5);
        }
    }

    private async Task<string> CallGeminiForChatAsync(string message, string context, List<ChatMessageDto> history, string language)
    {
        try
        {
            var apiKey = _configuration["Gemini:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return "Xin lỗi, dịch vụ AI hiện tại không khả dụng.";
            }

            var prompt = BuildChatPrompt(message, context, history, language);
            var requestBody = CreateApiRequestBody(prompt);

            using var httpContent = new System.Net.Http.StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            var apiEndpoint = _configuration["Gemini:ApiEndpoint"] ?? 
                "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";
            var requestUri = $"{apiEndpoint}?key={apiKey}";

            using var response = await _httpClient.PostAsync(requestUri, httpContent);

            if (!response.IsSuccessStatusCode)
            {
                return "Tôi đang gặp vấn đề kỹ thuật. Vui lòng thử lại sau.";
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            return ExtractTextFromGeminiResponse(responseContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Gemini API for chat");
            return "Xin lỗi, tôi không thể xử lý yêu cầu của bạn lúc này.";
        }
    }

    private string BuildChatPrompt(string message, string context, List<ChatMessageDto> history, string language)
    {
        var prompt = new StringBuilder();
        
        prompt.AppendLine(ChatbotKnowledgeBase.SystemPrompt);
        prompt.AppendLine();

        if (!string.IsNullOrEmpty(context))
        {
            prompt.AppendLine("THÔNG TIN LIÊN QUAN:");
            prompt.AppendLine(context);
            prompt.AppendLine();
        }

        if (history.Any())
        {
            prompt.AppendLine("LỊCH SỬ CUỘC TRÒ CHUYỆN:");
            foreach (var msg in history.TakeLast(5))
            {
                prompt.AppendLine($"{(msg.Role == "user" ? "Người dùng" : "DrHan AI")}: {msg.Content}");
            }
            prompt.AppendLine();
        }

        prompt.AppendLine($"CÂUHỎI HIỆN TẠI: {message}");
        prompt.AppendLine();
        prompt.AppendLine("Hãy trả lời câu hỏi một cách hữu ích và thân thiện:");

        return prompt.ToString();
    }

    private string BuildContextFromKnowledge(string category, string message)
    {
        var context = new StringBuilder();

        try
        {
            var relevantFeatures = ChatbotKnowledgeBase.AppFeatures
                .Where(kvp => IsRelevantToMessage(message, kvp.Value))
                .Take(2);

            foreach (var feature in relevantFeatures)
            {
                context.AppendLine(feature.Value);
                context.AppendLine();
            }

            if (category == "allergy" || ContainsAllergyKeywords(message))
            {
                var relevantAllergies = ChatbotKnowledgeBase.AllergyInformation
                    .Where(kvp => IsRelevantToMessage(message, kvp.Value))
                    .Take(2);

                foreach (var allergy in relevantAllergies)
                {
                    context.AppendLine(allergy.Value);
                    context.AppendLine();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building context from knowledge base");
        }

        return context.ToString();
    }

    private bool IsRelevantToMessage(string message, string content)
    {
        var messageLower = message.ToLower();
        var contentLower = content.ToLower();
        
        var messageWords = messageLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var relevantWords = messageWords.Where(word => 
            word.Length > 2 && contentLower.Contains(word)).Count();
        
        return relevantWords >= 2;
    }

    private bool ContainsAllergyKeywords(string message)
    {
        var allergyKeywords = new[] { "dị ứng", "allergen", "phản ứng", "sữa", "trứng", "đậu", "gluten", "hải sản" };
        var messageLower = message.ToLower();
        return allergyKeywords.Any(keyword => messageLower.Contains(keyword));
    }

    private async Task SaveMessageToHistoryAsync(string conversationId, string role, string content)
    {
        try
        {
            var cacheKey = $"conversation_history:{conversationId}";
            var history = await _cacheService.GetAsync<List<ChatMessageDto>>(cacheKey) ?? new List<ChatMessageDto>();

            history.Add(new ChatMessageDto
            {
                Role = role,
                Content = content,
                Timestamp = DateTime.Now
            });

            if (history.Count > 50)
            {
                history = history.OrderByDescending(x => x.Timestamp).Take(50).ToList();
            }

            await _cacheService.SetAsync(cacheKey, history, TimeSpan.FromHours(ConversationCacheDurationHours));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving message to history: {ConversationId}", conversationId);
        }
    }

    private List<SuggestedActionDto> GenerateSuggestedActions(string category, string message)
    {
        var actions = new List<SuggestedActionDto>();

        try
        {
            switch (category)
            {
                case "mealplan":
                    actions.Add(new SuggestedActionDto
                    {
                        Title = "Tạo kế hoạch bữa ăn",
                        Description = "Tạo kế hoạch bữa ăn phù hợp với dị ứng của bạn",
                        ActionType = "navigate",
                        ActionData = "/meal-plans/create"
                    });
                    break;

                case "allergy":
                    actions.Add(new SuggestedActionDto
                    {
                        Title = "Cập nhật hồ sơ dị ứng",
                        Description = "Cập nhật thông tin dị ứng thực phẩm",
                        ActionType = "navigate",
                        ActionData = "/profile/allergies"
                    });
                    break;

                case "app_help":
                    actions.Add(new SuggestedActionDto
                    {
                        Title = "Hướng dẫn sử dụng",
                        Description = "Xem hướng dẫn chi tiết về ứng dụng",
                        ActionType = "navigate",
                        ActionData = "/help"
                    });
                    break;
            }

            actions.Add(new SuggestedActionDto
            {
                Title = "Tìm công thức nấu ăn",
                Description = "Tìm món ăn an toàn cho bạn",
                ActionType = "navigate",
                ActionData = "/recipes/search"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating suggested actions");
        }

        return actions;
    }

    private bool IsEmergencyCase(string message)
    {
        var emergencyKeywords = new[] { "cấp cứu", "nguy hiểm", "khó thở", "sưng", "shock", "nghiêm trọng", "đau", "nôn" };
        var messageLower = message.ToLower();
        return emergencyKeywords.Any(keyword => messageLower.Contains(keyword));
    }

    private List<string> GetRelatedKeywords(string category)
    {
        return ChatbotKnowledgeBase.Keywords.TryGetValue(category, out var keywords) 
            ? keywords.Take(5).ToList() 
            : new List<string>();
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
                temperature = 0.3,
                maxOutputTokens = 2000,
                topP = 0.8,
                topK = 40
            }
        };
    }

    private string ExtractTextFromGeminiResponse(string responseContent)
    {
        try
        {
            var jsonDoc = JsonDocument.Parse(responseContent);
            var candidates = jsonDoc.RootElement.GetProperty("candidates");
            
            if (candidates.GetArrayLength() > 0)
            {
                var firstCandidate = candidates[0];
                var content = firstCandidate.GetProperty("content");
                var parts = content.GetProperty("parts");
                
                if (parts.GetArrayLength() > 0)
                {
                    var firstPart = parts[0];
                    return firstPart.GetProperty("text").GetString() ?? "Xin lỗi, tôi không thể tạo phản hồi.";
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text from Gemini response");
        }

        return "Xin lỗi, tôi gặp vấn đề khi xử lý phản hồi.";
    }
} 