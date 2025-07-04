namespace DrHan.Application.DTOs.Chatbot.RealTime;

public class RealTimeChatMessageDto
{
    /// <summary>
    /// Unique ID của tin nhắn
    /// </summary>
    public string MessageId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// ID cuộc trò chuyện
    /// </summary>
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// ID người gửi
    /// </summary>
    public string SenderId { get; set; } = string.Empty;

    /// <summary>
    /// Tên người gửi
    /// </summary>
    public string SenderName { get; set; } = string.Empty;

    /// <summary>
    /// Loại người gửi: user, assistant, system
    /// </summary>
    public string SenderType { get; set; } = "user";

    /// <summary>
    /// Nội dung tin nhắn
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Loại tin nhắn: text, typing, system, error
    /// </summary>
    public string MessageType { get; set; } = "text";

    /// <summary>
    /// Timestamp
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;

    /// <summary>
    /// Có phải tin nhắn đang được stream không
    /// </summary>
    public bool IsStreaming { get; set; } = false;

    /// <summary>
    /// Có phải tin nhắn cuối cùng trong stream không
    /// </summary>
    public bool IsStreamComplete { get; set; } = true;

    /// <summary>
    /// Metadata bổ sung
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}

public class RealTimeChatRequestDto
{
    /// <summary>
    /// Nội dung tin nhắn
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// ID cuộc trò chuyện
    /// </summary>
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// Ngôn ngữ
    /// </summary>
    public string Language { get; set; } = "vi";

    /// <summary>
    /// Loại chat: allergy, mealplan, app_help, general
    /// </summary>
    public string Category { get; set; } = "general";

    /// <summary>
    /// Có stream response không
    /// </summary>
    public bool EnableStreaming { get; set; } = true;

    /// <summary>
    /// ID người dùng
    /// </summary>
    public string? UserId { get; set; }
}

public class TypingIndicatorDto
{
    /// <summary>
    /// ID cuộc trò chuyện
    /// </summary>
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// ID người gửi
    /// </summary>
    public string SenderId { get; set; } = string.Empty;

    /// <summary>
    /// Tên người gửi
    /// </summary>
    public string SenderName { get; set; } = string.Empty;

    /// <summary>
    /// Có đang gõ không
    /// </summary>
    public bool IsTyping { get; set; }

    /// <summary>
    /// Timestamp
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

public class ChatConnectionDto
{
    /// <summary>
    /// Connection ID của SignalR
    /// </summary>
    public string ConnectionId { get; set; } = string.Empty;

    /// <summary>
    /// ID người dùng
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Tên người dùng
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// ID cuộc trò chuyện hiện tại
    /// </summary>
    public string? CurrentConversationId { get; set; }

    /// <summary>
    /// Thời gian kết nối
    /// </summary>
    public DateTime ConnectedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Trạng thái online
    /// </summary>
    public bool IsOnline { get; set; } = true;
}

public class ChatRoomDto
{
    /// <summary>
    /// ID cuộc trò chuyện
    /// </summary>
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// Tên cuộc trò chuyện
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Danh sách người tham gia
    /// </summary>
    public List<ChatConnectionDto> Participants { get; set; } = new();

    /// <summary>
    /// Thời gian tạo
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Tin nhắn cuối cùng
    /// </summary>
    public RealTimeChatMessageDto? LastMessage { get; set; }

    /// <summary>
    /// Số lượng tin nhắn chưa đọc
    /// </summary>
    public int UnreadCount { get; set; } = 0;
} 