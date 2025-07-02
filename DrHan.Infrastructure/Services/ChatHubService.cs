using DrHan.Application.DTOs.Chatbot.RealTime;
using DrHan.Application.Interfaces.Services;
using DrHan.Application.Interfaces.Services.CacheService;
using DrHan.Infrastructure.KnowledgeBase;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace DrHan.Infrastructure.Services;

public class ChatHubService : IChatHubService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ICacheService _cacheService;
    private readonly ILogger<ChatHubService> _logger;

    private const int StreamingDelayMs = 50; // Delay between streamed chunks
    private const int ConnectionCacheDurationHours = 24;
    private const int MessageCacheDurationHours = 48;

    public ChatHubService(
        HttpClient httpClient,
        IConfiguration configuration,
        ICacheService cacheService,
        ILogger<ChatHubService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async IAsyncEnumerable<RealTimeChatMessageDto> ProcessRealTimeMessageAsync(
        RealTimeChatRequestDto request, 
        string connectionId)
    {
        _logger.LogInformation("Processing real-time message from connection {ConnectionId}: {Message}", 
            connectionId, request.Message);

        await foreach (var message in ProcessRealTimeMessageInternalAsync(request, connectionId))
        {
            yield return message;
        }
    }

    private async IAsyncEnumerable<RealTimeChatMessageDto> ProcessRealTimeMessageInternalAsync(
        RealTimeChatRequestDto request, 
        string connectionId)
    {
        // Lưu tin nhắn của user
        var userMessage = new RealTimeChatMessageDto
        {
            ConversationId = request.ConversationId,
            SenderId = request.UserId ?? connectionId,
            SenderName = "User",
            SenderType = "user",
            Content = request.Message,
            MessageType = "text",
            IsStreamComplete = true
        };
        await SaveMessageAsync(userMessage);

        // Phân tích intent
        var (category, confidence, suggestedActions) = await AnalyzeMessageIntentAsync(request.Message, request.Language);

        // Kiểm tra tình huống khẩn cấp
        var isEmergency = await IsEmergencyMessageAsync(request.Message);

        // Gửi typing indicator
        yield return new RealTimeChatMessageDto
        {
            ConversationId = request.ConversationId,
            SenderId = "drhan-ai",
            SenderName = "DrHan AI",
            SenderType = "assistant",
            Content = "đang soạn tin nhắn...",
            MessageType = "typing",
            IsStreaming = true,
            IsStreamComplete = false
        };

        if (isEmergency)
        {
            // Gửi cảnh báo khẩn cấp ngay lập tức
            var emergencyMessage = new RealTimeChatMessageDto
            {
                ConversationId = request.ConversationId,
                SenderId = "drhan-ai",
                SenderName = "DrHan AI",
                SenderType = "assistant",
                Content = "🚨 **TÌNH HUỐNG KHẨN CẤP** 🚨\n\nTôi phát hiện bạn có thể đang gặp tình huống khẩn cấp về dị ứng thực phẩm.\n\n**LIÊN HỆ NGAY:**\n📞 Cấp cứu: 115\n🏥 Bệnh viện gần nhất\n\n**HÀNH ĐỘNG NGAY:**\n- Nếu có bút tiêm epinephrine, sử dụng ngay\n- Giữ bình tĩnh\n- Đến bệnh viện ngay lập tức",
                MessageType = "emergency",
                IsStreamComplete = true,
                Metadata = new Dictionary<string, object>
                {
                    ["emergency"] = true,
                    ["category"] = category,
                    ["confidence"] = confidence
                }
            };

            await SaveMessageAsync(emergencyMessage);
            await SendEmergencyAlertAsync(emergencyMessage, connectionId);
            yield return emergencyMessage;
            yield break;
        }

        // Stream AI response
        var context = BuildContextFromKnowledge(category, request.Message);
        var history = await GetMessageHistoryAsync(request.ConversationId, 10);

        await foreach (var chunk in StreamGeminiResponseAsync(request.Message, context, history, request.Language))
        {
            yield return chunk;
            await Task.Delay(StreamingDelayMs);
        }

        // Gửi suggested actions sau khi hoàn thành response
        if (suggestedActions.Any())
        {
            yield return new RealTimeChatMessageDto
            {
                ConversationId = request.ConversationId,
                SenderId = "drhan-ai",
                SenderName = "DrHan AI",
                SenderType = "system",
                Content = "Các hành động gợi ý:",
                MessageType = "actions",
                IsStreamComplete = true,
                Metadata = new Dictionary<string, object>
                {
                    ["suggestedActions"] = suggestedActions,
                    ["category"] = category
                }
            };
        }
    }

    private async IAsyncEnumerable<RealTimeChatMessageDto> StreamGeminiResponseAsync(
        string message, 
        string context, 
        List<RealTimeChatMessageDto> history, 
        string language)
    {
        var apiKey = _configuration["Gemini:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            yield return new RealTimeChatMessageDto
            {
                Content = "Dịch vụ AI hiện tại không khả dụng.",
                MessageType = "error",
                IsStreamComplete = true
            };
            yield break;
        }

        var prompt = BuildChatPrompt(message, context, history, language);
        var requestBody = CreateStreamingApiRequestBody(prompt);

        using var httpContent = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json");

        var apiEndpoint = _configuration["Gemini:ApiEndpoint"] ?? 
            "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";
        var requestUri = $"{apiEndpoint}?key={apiKey}";

        using var response = await _httpClient.PostAsync(requestUri, httpContent);

        if (!response.IsSuccessStatusCode)
        {
            yield return new RealTimeChatMessageDto
            {
                Content = "Tôi đang gặp vấn đề kỹ thuật. Vui lòng thử lại sau.",
                MessageType = "error",
                IsStreamComplete = true
            };
            yield break;
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        var fullResponse = ExtractTextFromGeminiResponse(responseContent);

        // Simulate streaming by breaking response into chunks
        var words = fullResponse.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var currentChunk = new StringBuilder();
        var messageId = Guid.NewGuid().ToString();

        for (int i = 0; i < words.Length; i++)
        {
            currentChunk.Append(words[i] + " ");

            // Send chunk every 3-5 words or at the end
            if (i % 4 == 0 || i == words.Length - 1)
            {
                var isComplete = i == words.Length - 1;
                var chunk = new RealTimeChatMessageDto
                {
                    MessageId = messageId,
                    SenderId = "drhan-ai",
                    SenderName = "DrHan AI",
                    SenderType = "assistant",
                    Content = currentChunk.ToString().Trim(),
                    MessageType = "text",
                    IsStreaming = !isComplete,
                    IsStreamComplete = isComplete
                };

                if (isComplete)
                {
                    // Lưu tin nhắn hoàn chỉnh
                    await SaveMessageAsync(chunk);
                }

                yield return chunk;
            }
        }
    }

    public async Task AddConnectionAsync(ChatConnectionDto connection)
    {
        try
        {
            var cacheKey = $"chat_connection:{connection.ConnectionId}";
            await _cacheService.SetAsync(cacheKey, connection, TimeSpan.FromHours(ConnectionCacheDurationHours));
            
            _logger.LogInformation("Added chat connection: {ConnectionId} for user {UserId}", 
                connection.ConnectionId, connection.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding connection: {ConnectionId}", connection.ConnectionId);
        }
    }

    public async Task RemoveConnectionAsync(string connectionId)
    {
        try
        {
            var cacheKey = $"chat_connection:{connectionId}";
            await _cacheService.RemoveAsync(cacheKey);
            
            _logger.LogInformation("Removed chat connection: {ConnectionId}", connectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing connection: {ConnectionId}", connectionId);
        }
    }

    public async Task JoinConversationAsync(string connectionId, string conversationId)
    {
        try
        {
            var connection = await GetConnectionAsync(connectionId);
            if (connection != null)
            {
                connection.CurrentConversationId = conversationId;
                await UpdateConnectionAsync(connection);
            }

            // Add to conversation participants
            var conversationKey = $"conversation_participants:{conversationId}";
            var participants = await _cacheService.GetAsync<List<string>>(conversationKey) ?? new List<string>();
            
            if (!participants.Contains(connectionId))
            {
                participants.Add(connectionId);
                await _cacheService.SetAsync(conversationKey, participants, TimeSpan.FromHours(24));
            }

            _logger.LogInformation("Connection {ConnectionId} joined conversation {ConversationId}", 
                connectionId, conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining conversation: {ConnectionId} -> {ConversationId}", 
                connectionId, conversationId);
        }
    }

    public async Task LeaveConversationAsync(string connectionId, string conversationId)
    {
        try
        {
            var connection = await GetConnectionAsync(connectionId);
            if (connection != null)
            {
                connection.CurrentConversationId = null;
                await UpdateConnectionAsync(connection);
            }

            // Remove from conversation participants
            var conversationKey = $"conversation_participants:{conversationId}";
            var participants = await _cacheService.GetAsync<List<string>>(conversationKey) ?? new List<string>();
            
            participants.Remove(connectionId);
            await _cacheService.SetAsync(conversationKey, participants, TimeSpan.FromHours(24));

            _logger.LogInformation("Connection {ConnectionId} left conversation {ConversationId}", 
                connectionId, conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving conversation: {ConnectionId} -> {ConversationId}", 
                connectionId, conversationId);
        }
    }

    public async Task SendTypingIndicatorAsync(TypingIndicatorDto typingIndicator)
    {
        try
        {
            var cacheKey = $"typing_indicator:{typingIndicator.ConversationId}:{typingIndicator.SenderId}";
            
            if (typingIndicator.IsTyping)
            {
                await _cacheService.SetAsync(cacheKey, typingIndicator, TimeSpan.FromSeconds(30));
            }
            else
            {
                await _cacheService.RemoveAsync(cacheKey);
            }

            _logger.LogDebug("Typing indicator for {SenderId} in {ConversationId}: {IsTyping}", 
                typingIndicator.SenderId, typingIndicator.ConversationId, typingIndicator.IsTyping);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending typing indicator");
        }
    }

    public async Task<List<ChatConnectionDto>> GetOnlineUsersInConversationAsync(string conversationId)
    {
        try
        {
            var conversationKey = $"conversation_participants:{conversationId}";
            var participantIds = await _cacheService.GetAsync<List<string>>(conversationKey) ?? new List<string>();
            
            var onlineUsers = new List<ChatConnectionDto>();
            
            foreach (var connectionId in participantIds)
            {
                var connection = await GetConnectionAsync(connectionId);
                if (connection?.IsOnline == true)
                {
                    onlineUsers.Add(connection);
                }
            }

            return onlineUsers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting online users for conversation: {ConversationId}", conversationId);
            return new List<ChatConnectionDto>();
        }
    }

    public async Task<ChatConnectionDto?> GetConnectionAsync(string connectionId)
    {
        try
        {
            var cacheKey = $"chat_connection:{connectionId}";
            return await _cacheService.GetAsync<ChatConnectionDto>(cacheKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting connection: {ConnectionId}", connectionId);
            return null;
        }
    }

    public async Task UpdateConnectionAsync(ChatConnectionDto connection)
    {
        try
        {
            var cacheKey = $"chat_connection:{connection.ConnectionId}";
            await _cacheService.SetAsync(cacheKey, connection, TimeSpan.FromHours(ConnectionCacheDurationHours));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating connection: {ConnectionId}", connection.ConnectionId);
        }
    }

    public async Task<List<ChatRoomDto>> GetUserConversationsAsync(string userId)
    {
        try
        {
            var cacheKey = $"user_conversations:{userId}";
            return await _cacheService.GetAsync<List<ChatRoomDto>>(cacheKey) ?? new List<ChatRoomDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user conversations: {UserId}", userId);
            return new List<ChatRoomDto>();
        }
    }

    public async Task<ChatRoomDto> CreateConversationAsync(ChatRoomDto room)
    {
        try
        {
            var cacheKey = $"conversation:{room.ConversationId}";
            await _cacheService.SetAsync(cacheKey, room, TimeSpan.FromDays(30));

            // Add to user's conversation list
            foreach (var participant in room.Participants)
            {
                var userConversationsKey = $"user_conversations:{participant.UserId}";
                var userConversations = await _cacheService.GetAsync<List<ChatRoomDto>>(userConversationsKey) ?? new List<ChatRoomDto>();
                
                if (!userConversations.Any(c => c.ConversationId == room.ConversationId))
                {
                    userConversations.Add(room);
                    await _cacheService.SetAsync(userConversationsKey, userConversations, TimeSpan.FromDays(30));
                }
            }

            _logger.LogInformation("Created conversation: {ConversationId}", room.ConversationId);
            return room;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating conversation: {ConversationId}", room.ConversationId);
            throw;
        }
    }

    public async Task SaveMessageAsync(RealTimeChatMessageDto message)
    {
        try
        {
            var cacheKey = $"realtime_messages:{message.ConversationId}";
            var messages = await _cacheService.GetAsync<List<RealTimeChatMessageDto>>(cacheKey) ?? new List<RealTimeChatMessageDto>();
            
            messages.Add(message);
            
            // Keep only last 100 messages
            if (messages.Count > 100)
            {
                messages = messages.OrderByDescending(m => m.Timestamp).Take(100).ToList();
            }

            await _cacheService.SetAsync(cacheKey, messages, TimeSpan.FromHours(MessageCacheDurationHours));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving message: {MessageId}", message.MessageId);
        }
    }

    public async Task<List<RealTimeChatMessageDto>> GetMessageHistoryAsync(string conversationId, int limit = 50)
    {
        try
        {
            var cacheKey = $"realtime_messages:{conversationId}";
            var messages = await _cacheService.GetAsync<List<RealTimeChatMessageDto>>(cacheKey) ?? new List<RealTimeChatMessageDto>();
            
            return messages.OrderByDescending(m => m.Timestamp)
                          .Take(limit)
                          .OrderBy(m => m.Timestamp)
                          .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting message history: {ConversationId}", conversationId);
            return new List<RealTimeChatMessageDto>();
        }
    }

    public async Task ClearConversationHistoryAsync(string conversationId)
    {
        try
        {
            var cacheKey = $"realtime_messages:{conversationId}";
            await _cacheService.RemoveAsync(cacheKey);
            
            _logger.LogInformation("Cleared conversation history: {ConversationId}", conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing conversation history: {ConversationId}", conversationId);
        }
    }

    public async Task<(string category, double confidence, List<Dictionary<string, string>> suggestedActions)> 
        AnalyzeMessageIntentAsync(string message, string language = "vi")
    {
        try
        {
            // Reuse existing intent analysis logic
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

            // Generate suggested actions
            var suggestedActions = GenerateSuggestedActions(category);

            return (category, confidence, suggestedActions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing message intent");
            return ("general", 0.5, new List<Dictionary<string, string>>());
        }
    }

    public async Task<bool> IsEmergencyMessageAsync(string message)
    {
        try
        {
            var emergencyKeywords = new[] { "cấp cứu", "nguy hiểm", "khó thở", "sưng", "shock", "nghiêm trọng", "đau", "nôn", "bệnh viện" };
            var messageLower = message.ToLower();
            
            return emergencyKeywords.Any(keyword => messageLower.Contains(keyword));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking emergency message");
            return false;
        }
    }

    public async Task SendEmergencyAlertAsync(RealTimeChatMessageDto message, string connectionId)
    {
        try
        {
            var connection = await GetConnectionAsync(connectionId);
            
            _logger.LogCritical("EMERGENCY ALERT - User {UserId} ({ConnectionId}) sent emergency message: {Message}", 
                connection?.UserId ?? "Unknown", connectionId, message.Content);

            // Here you could implement additional emergency alerting:
            // - Send to admin dashboard
            // - Send SMS to emergency contacts
            // - Log to emergency tracking system
            // - Send push notifications to healthcare providers
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending emergency alert");
        }
    }

    #region Private Helper Methods

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

    private string BuildChatPrompt(string message, string context, List<RealTimeChatMessageDto> history, string language)
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
            foreach (var msg in history.Where(m => m.MessageType == "text").TakeLast(5))
            {
                var roleText = msg.SenderType == "user" ? "Người dùng" : "DrHan AI";
                prompt.AppendLine($"{roleText}: {msg.Content}");
            }
            prompt.AppendLine();
        }

        prompt.AppendLine($"CÂUHỎI HIỆN TẠI: {message}");
        prompt.AppendLine();
        prompt.AppendLine("Hãy trả lời câu hỏi một cách hữu ích và thân thiện:");

        return prompt.ToString();
    }

    private List<Dictionary<string, string>> GenerateSuggestedActions(string category)
    {
        var actions = new List<Dictionary<string, string>>();

        try
        {
            switch (category)
            {
                case "mealplan":
                    actions.Add(new Dictionary<string, string>
                    {
                        ["title"] = "Tạo kế hoạch bữa ăn",
                        ["description"] = "Tạo kế hoạch bữa ăn phù hợp với dị ứng của bạn",
                        ["action"] = "navigate",
                        ["data"] = "/meal-plans/create"
                    });
                    break;

                case "allergy":
                    actions.Add(new Dictionary<string, string>
                    {
                        ["title"] = "Cập nhật hồ sơ dị ứng",
                        ["description"] = "Cập nhật thông tin dị ứng thực phẩm",
                        ["action"] = "navigate",
                        ["data"] = "/profile/allergies"
                    });
                    break;

                case "app_help":
                    actions.Add(new Dictionary<string, string>
                    {
                        ["title"] = "Hướng dẫn sử dụng",
                        ["description"] = "Xem hướng dẫn chi tiết về ứng dụng",
                        ["action"] = "navigate",
                        ["data"] = "/help"
                    });
                    break;
            }

            actions.Add(new Dictionary<string, string>
            {
                ["title"] = "Tìm công thức nấu ăn",
                ["description"] = "Tìm món ăn an toàn cho bạn",
                ["action"] = "navigate",
                ["data"] = "/recipes/search"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating suggested actions");
        }

        return actions;
    }

    private static object CreateStreamingApiRequestBody(string prompt)
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

    #endregion
} 