namespace DrHan.Application.DTOs.Chatbot;

public class ChatbotResponseDto
{
    /// <summary>
    /// Phản hồi của AI chatbot
    /// </summary>
    public string Response { get; set; } = string.Empty;

    /// <summary>
    /// ID cuộc trò chuyện
    /// </summary>
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp của phản hồi
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;

    /// <summary>
    /// Độ tin cậy của phản hồi (0-1)
    /// </summary>
    public double Confidence { get; set; } = 1.0;

    /// <summary>
    /// Các hành động được đề xuất
    /// </summary>
    public List<SuggestedActionDto>? SuggestedActions { get; set; }

    /// <summary>
    /// Danh mục của câu hỏi được xử lý
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Có cần chuyển đến hỗ trợ con người không
    /// </summary>
    public bool RequiresHumanSupport { get; set; } = false;

    /// <summary>
    /// Các từ khóa liên quan để tìm kiếm thêm
    /// </summary>
    public List<string>? RelatedKeywords { get; set; }
}

public class SuggestedActionDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty; // "navigate", "search", "external_link"
    public string ActionData { get; set; } = string.Empty; // URL hoặc data cần thiết
} 