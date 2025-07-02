using DrHan.Application.DTOs.Chatbot.RealTime;

namespace DrHan.Application.Interfaces.Services;

public interface IChatHubService
{
    /// <summary>
    /// Xử lý tin nhắn real-time và stream response
    /// </summary>
    /// <param name="request">Yêu cầu chat real-time</param>
    /// <param name="connectionId">ID kết nối SignalR</param>
    /// <returns>Async enumerable để stream response</returns>
    IAsyncEnumerable<RealTimeChatMessageDto> ProcessRealTimeMessageAsync(
        RealTimeChatRequestDto request, 
        string connectionId);

    /// <summary>
    /// Thêm kết nối mới vào chat hub
    /// </summary>
    /// <param name="connection">Thông tin kết nối</param>
    Task AddConnectionAsync(ChatConnectionDto connection);

    /// <summary>
    /// Xóa kết nối khỏi chat hub
    /// </summary>
    /// <param name="connectionId">ID kết nối</param>
    Task RemoveConnectionAsync(string connectionId);

    /// <summary>
    /// Tham gia vào cuộc trò chuyện
    /// </summary>
    /// <param name="connectionId">ID kết nối</param>
    /// <param name="conversationId">ID cuộc trò chuyện</param>
    Task JoinConversationAsync(string connectionId, string conversationId);

    /// <summary>
    /// Rời khỏi cuộc trò chuyện
    /// </summary>
    /// <param name="connectionId">ID kết nối</param>
    /// <param name="conversationId">ID cuộc trò chuyện</param>
    Task LeaveConversationAsync(string connectionId, string conversationId);

    /// <summary>
    /// Gửi typing indicator
    /// </summary>
    /// <param name="typingIndicator">Thông tin typing</param>
    Task SendTypingIndicatorAsync(TypingIndicatorDto typingIndicator);

    /// <summary>
    /// Lấy danh sách người dùng online trong cuộc trò chuyện
    /// </summary>
    /// <param name="conversationId">ID cuộc trò chuyện</param>
    /// <returns>Danh sách kết nối online</returns>
    Task<List<ChatConnectionDto>> GetOnlineUsersInConversationAsync(string conversationId);

    /// <summary>
    /// Lấy thông tin kết nối
    /// </summary>
    /// <param name="connectionId">ID kết nối</param>
    /// <returns>Thông tin kết nối</returns>
    Task<ChatConnectionDto?> GetConnectionAsync(string connectionId);

    /// <summary>
    /// Cập nhật thông tin kết nối
    /// </summary>
    /// <param name="connection">Thông tin kết nối mới</param>
    Task UpdateConnectionAsync(ChatConnectionDto connection);

    /// <summary>
    /// Lấy danh sách cuộc trò chuyện của người dùng
    /// </summary>
    /// <param name="userId">ID người dùng</param>
    /// <returns>Danh sách cuộc trò chuyện</returns>
    Task<List<ChatRoomDto>> GetUserConversationsAsync(string userId);

    /// <summary>
    /// Tạo cuộc trò chuyện mới
    /// </summary>
    /// <param name="room">Thông tin cuộc trò chuyện</param>
    Task<ChatRoomDto> CreateConversationAsync(ChatRoomDto room);

    /// <summary>
    /// Lưu tin nhắn vào lịch sử
    /// </summary>
    /// <param name="message">Tin nhắn cần lưu</param>
    Task SaveMessageAsync(RealTimeChatMessageDto message);

    /// <summary>
    /// Lấy lịch sử tin nhắn real-time
    /// </summary>
    /// <param name="conversationId">ID cuộc trò chuyện</param>
    /// <param name="limit">Số lượng tin nhắn</param>
    /// <returns>Danh sách tin nhắn</returns>
    Task<List<RealTimeChatMessageDto>> GetMessageHistoryAsync(string conversationId, int limit = 50);

    /// <summary>
    /// Xóa lịch sử cuộc trò chuyện real-time
    /// </summary>
    /// <param name="conversationId">ID cuộc trò chuyện</param>
    Task ClearConversationHistoryAsync(string conversationId);

    /// <summary>
    /// Phân tích intent và tạo suggested actions
    /// </summary>
    /// <param name="message">Tin nhắn cần phân tích</param>
    /// <param name="language">Ngôn ngữ</param>
    /// <returns>Intent và suggested actions</returns>
    Task<(string category, double confidence, List<Dictionary<string, string>> suggestedActions)> 
        AnalyzeMessageIntentAsync(string message, string language = "vi");

    /// <summary>
    /// Kiểm tra tình huống khẩn cấp
    /// </summary>
    /// <param name="message">Tin nhắn cần kiểm tra</param>
    /// <returns>True nếu là tình huống khẩn cấp</returns>
    Task<bool> IsEmergencyMessageAsync(string message);

    /// <summary>
    /// Gửi cảnh báo khẩn cấp tới admin
    /// </summary>
    /// <param name="message">Tin nhắn khẩn cấp</param>
    /// <param name="connectionId">ID kết nối người gửi</param>
    Task SendEmergencyAlertAsync(RealTimeChatMessageDto message, string connectionId);
} 