using System.ComponentModel.DataAnnotations;

namespace DrHan.Application.DTOs.Chatbot;

public class ChatbotRequestDto
{
    [Required(ErrorMessage = "Tin nhắn không được để trống")]
    [StringLength(1000, ErrorMessage = "Tin nhắn không được vượt quá 1000 ký tự")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// ID của cuộc trò chuyện để duy trì ngữ cảnh
    /// </summary>
    public string? ConversationId { get; set; }

    /// <summary>
    /// Lịch sử trò chuyện gần đây (tối đa 10 tin nhắn cuối)
    /// </summary>
    public List<ChatMessageDto>? ConversationHistory { get; set; }

    /// <summary>
    /// ID người dùng để cá nhân hóa phản hồi
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Ngôn ngữ ưa thích (mặc định: vi)
    /// </summary>
    public string Language { get; set; } = "vi";

    /// <summary>
    /// Loại hỏi đáp: general, allergy, mealplan, app_help
    /// </summary>
    public string? Category { get; set; }
}

public class ChatMessageDto
{
    public string Role { get; set; } = string.Empty; // "user" hoặc "assistant"
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
} 