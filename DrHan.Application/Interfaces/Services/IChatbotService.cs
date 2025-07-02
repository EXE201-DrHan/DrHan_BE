using DrHan.Application.DTOs.Chatbot;

namespace DrHan.Application.Interfaces.Services;

public interface IChatbotService
{
    /// <summary>
    /// Xử lý tin nhắn của người dùng và trả về phản hồi từ AI
    /// </summary>
    /// <param name="request">Yêu cầu chat từ người dùng</param>
    /// <returns>Phản hồi từ chatbot</returns>
    Task<ChatbotResponseDto> ProcessMessageAsync(ChatbotRequestDto request);

    /// <summary>
    /// Lấy lịch sử cuộc trò chuyện
    /// </summary>
    /// <param name="conversationId">ID cuộc trò chuyện</param>
    /// <param name="limit">Số lượng tin nhắn tối đa</param>
    /// <returns>Lịch sử trò chuyện</returns>
    Task<List<ChatMessageDto>> GetConversationHistoryAsync(string conversationId, int limit = 20);

    /// <summary>
    /// Xóa lịch sử cuộc trò chuyện
    /// </summary>
    /// <param name="conversationId">ID cuộc trò chuyện</param>
    Task ClearConversationAsync(string conversationId);

    /// <summary>
    /// Lấy các câu hỏi gợi ý dựa trên danh mục
    /// </summary>
    /// <param name="category">Danh mục: allergy, mealplan, app_help, general</param>
    /// <param name="language">Ngôn ngữ (mặc định: vi)</param>
    /// <returns>Danh sách câu hỏi gợi ý</returns>
    Task<List<string>> GetSuggestedQuestionsAsync(string category, string language = "vi");

    /// <summary>
    /// Phân tích intent của tin nhắn
    /// </summary>
    /// <param name="message">Tin nhắn cần phân tích</param>
    /// <param name="language">Ngôn ngữ</param>
    /// <returns>Danh mục và độ tin cậy</returns>
    Task<(string category, double confidence)> AnalyzeIntentAsync(string message, string language = "vi");
} 