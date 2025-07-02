using DrHan.Application.DTOs.Chatbot.RealTime;
using DrHan.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Runtime.CompilerServices;

namespace DrHan.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IChatHubService _chatHubService;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(IChatHubService chatHubService, ILogger<ChatHub> logger)
    {
        _chatHubService = chatHubService ?? throw new ArgumentNullException(nameof(chatHubService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Connection Management

    public override async Task OnConnectedAsync()
    {
        try
        {
            var userId = Context.UserIdentifier ?? Context.ConnectionId;
            var userName = Context.User?.Identity?.Name ?? "Guest";

            var connection = new ChatConnectionDto
            {
                ConnectionId = Context.ConnectionId,
                UserId = userId,
                UserName = userName,
                ConnectedAt = DateTime.UtcNow,
                IsOnline = true
            };

            await _chatHubService.AddConnectionAsync(connection);

            // Notify user of successful connection
            await Clients.Caller.SendAsync("ConnectionEstablished", new
            {
                ConnectionId = Context.ConnectionId,
                UserId = userId,
                UserName = userName,
                Message = "Kết nối thành công! Bạn có thể bắt đầu trò chuyện với DrHan AI.",
                ConnectedAt = DateTime.UtcNow
            });

            _logger.LogInformation("User {UserId} connected with connection {ConnectionId}", userId, Context.ConnectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in OnConnectedAsync for connection {ConnectionId}", Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            var connection = await _chatHubService.GetConnectionAsync(Context.ConnectionId);
            
            if (connection?.CurrentConversationId != null)
            {
                await LeaveConversation(connection.CurrentConversationId);
            }

            await _chatHubService.RemoveConnectionAsync(Context.ConnectionId);

            _logger.LogInformation("User disconnected: {ConnectionId}", Context.ConnectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in OnDisconnectedAsync for connection {ConnectionId}", Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    #endregion

    #region Conversation Management

    /// <summary>
    /// Tham gia vào cuộc trò chuyện
    /// </summary>
    /// <param name="conversationId">ID cuộc trò chuyện</param>
    public async Task JoinConversation(string conversationId)
    {
        try
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Conversation_{conversationId}");
            await _chatHubService.JoinConversationAsync(Context.ConnectionId, conversationId);

            var connection = await _chatHubService.GetConnectionAsync(Context.ConnectionId);
            
            // Notify others in the conversation
            await Clients.Group($"Conversation_{conversationId}")
                .SendAsync("UserJoined", new
                {
                    ConversationId = conversationId,
                    UserId = connection?.UserId,
                    UserName = connection?.UserName,
                    ConnectionId = Context.ConnectionId,
                    JoinedAt = DateTime.UtcNow
                });

            // Send conversation history to the joining user
            var history = await _chatHubService.GetMessageHistoryAsync(conversationId);
            await Clients.Caller.SendAsync("ConversationHistory", history);

            _logger.LogInformation("Connection {ConnectionId} joined conversation {ConversationId}", 
                Context.ConnectionId, conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining conversation {ConversationId} for connection {ConnectionId}", 
                conversationId, Context.ConnectionId);
            
            await Clients.Caller.SendAsync("Error", new
            {
                Type = "JoinConversationError",
                Message = "Không thể tham gia cuộc trò chuyện. Vui lòng thử lại.",
                ConversationId = conversationId
            });
        }
    }

    /// <summary>
    /// Rời khỏi cuộc trò chuyện
    /// </summary>
    /// <param name="conversationId">ID cuộc trò chuyện</param>
    public async Task LeaveConversation(string conversationId)
    {
        try
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Conversation_{conversationId}");
            await _chatHubService.LeaveConversationAsync(Context.ConnectionId, conversationId);

            var connection = await _chatHubService.GetConnectionAsync(Context.ConnectionId);

            // Notify others in the conversation
            await Clients.Group($"Conversation_{conversationId}")
                .SendAsync("UserLeft", new
                {
                    ConversationId = conversationId,
                    UserId = connection?.UserId,
                    UserName = connection?.UserName,
                    ConnectionId = Context.ConnectionId,
                    LeftAt = DateTime.UtcNow
                });

            _logger.LogInformation("Connection {ConnectionId} left conversation {ConversationId}", 
                Context.ConnectionId, conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving conversation {ConversationId} for connection {ConnectionId}", 
                conversationId, Context.ConnectionId);
        }
    }

    /// <summary>
    /// Lấy danh sách người dùng online trong cuộc trò chuyện
    /// </summary>
    /// <param name="conversationId">ID cuộc trò chuyện</param>
    public async Task GetOnlineUsers(string conversationId)
    {
        try
        {
            var onlineUsers = await _chatHubService.GetOnlineUsersInConversationAsync(conversationId);
            await Clients.Caller.SendAsync("OnlineUsers", onlineUsers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting online users for conversation {ConversationId}", conversationId);
        }
    }

    #endregion

    #region Real-time Messaging

    /// <summary>
    /// Gửi tin nhắn real-time với streaming response
    /// </summary>
    /// <param name="request">Yêu cầu chat real-time</param>
    public async Task SendMessage(RealTimeChatRequestDto request)
    {
        try
        {
            _logger.LogInformation("Received real-time message from {ConnectionId}: {Message}", 
                Context.ConnectionId, request.Message);

            // Validate request
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                await Clients.Caller.SendAsync("Error", new
                {
                    Type = "InvalidMessage",
                    Message = "Tin nhắn không được để trống."
                });
                return;
            }

            if (string.IsNullOrWhiteSpace(request.ConversationId))
            {
                request.ConversationId = $"chat_{Context.ConnectionId}_{DateTime.UtcNow:yyyyMMddHHmmss}";
            }

            // Get user information
            var connection = await _chatHubService.GetConnectionAsync(Context.ConnectionId);
            request.UserId = connection?.UserId ?? Context.ConnectionId;

            // Broadcast user message to conversation group
            var userMessage = new RealTimeChatMessageDto
            {
                ConversationId = request.ConversationId,
                SenderId = request.UserId,
                SenderName = connection?.UserName ?? "User",
                SenderType = "user",
                Content = request.Message,
                MessageType = "text",
                IsStreamComplete = true
            };

            await Clients.Group($"Conversation_{request.ConversationId}")
                .SendAsync("MessageReceived", userMessage);

            // Process and stream AI response
            await StreamAIResponse(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message from connection {ConnectionId}", Context.ConnectionId);
            
            await Clients.Caller.SendAsync("Error", new
            {
                Type = "MessageProcessingError",
                Message = "Đã xảy ra lỗi khi xử lý tin nhắn. Vui lòng thử lại.",
                ConversationId = request.ConversationId
            });
        }
    }

    /// <summary>
    /// Stream AI response to clients
    /// </summary>
    /// <param name="request">Yêu cầu chat</param>
    private async Task StreamAIResponse(RealTimeChatRequestDto request)
    {
        try
        {
            await foreach (var chunk in _chatHubService.ProcessRealTimeMessageAsync(request, Context.ConnectionId))
            {
                // Send to conversation group
                await Clients.Group($"Conversation_{request.ConversationId}")
                    .SendAsync("MessageReceived", chunk);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error streaming AI response for conversation {ConversationId}", request.ConversationId);
            
            var errorMessage = new RealTimeChatMessageDto
            {
                ConversationId = request.ConversationId,
                SenderId = "drhan-ai",
                SenderName = "DrHan AI",
                SenderType = "assistant",
                Content = "Xin lỗi, tôi đang gặp vấn đề kỹ thuật. Vui lòng thử lại sau.",
                MessageType = "error",
                IsStreamComplete = true
            };

            await Clients.Group($"Conversation_{request.ConversationId}")
                .SendAsync("MessageReceived", errorMessage);
        }
    }

    /// <summary>
    /// Gửi typing indicator
    /// </summary>
    /// <param name="typingIndicator">Thông tin typing</param>
    public async Task SendTypingIndicator(TypingIndicatorDto typingIndicator)
    {
        try
        {
            var connection = await _chatHubService.GetConnectionAsync(Context.ConnectionId);
            
            typingIndicator.SenderId = connection?.UserId ?? Context.ConnectionId;
            typingIndicator.SenderName = connection?.UserName ?? "User";
            typingIndicator.Timestamp = DateTime.UtcNow;

            await _chatHubService.SendTypingIndicatorAsync(typingIndicator);

            // Broadcast to others in the conversation (not to sender)
            await Clients.GroupExcept($"Conversation_{typingIndicator.ConversationId}", Context.ConnectionId)
                .SendAsync("TypingIndicator", typingIndicator);

            _logger.LogDebug("Typing indicator sent from {ConnectionId} for conversation {ConversationId}: {IsTyping}", 
                Context.ConnectionId, typingIndicator.ConversationId, typingIndicator.IsTyping);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending typing indicator from connection {ConnectionId}", Context.ConnectionId);
        }
    }

    #endregion

    #region Conversation History

    /// <summary>
    /// Lấy lịch sử tin nhắn
    /// </summary>
    /// <param name="conversationId">ID cuộc trò chuyện</param>
    /// <param name="limit">Số lượng tin nhắn</param>
    public async Task GetMessageHistory(string conversationId, int limit = 50)
    {
        try
        {
            var history = await _chatHubService.GetMessageHistoryAsync(conversationId, limit);
            await Clients.Caller.SendAsync("MessageHistory", history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting message history for conversation {ConversationId}", conversationId);
            
            await Clients.Caller.SendAsync("Error", new
            {
                Type = "HistoryError",
                Message = "Không thể tải lịch sử tin nhắn.",
                ConversationId = conversationId
            });
        }
    }

    /// <summary>
    /// Xóa lịch sử cuộc trò chuyện
    /// </summary>
    /// <param name="conversationId">ID cuộc trò chuyện</param>
    public async Task ClearConversationHistory(string conversationId)
    {
        try
        {
            await _chatHubService.ClearConversationHistoryAsync(conversationId);
            
            // Notify all participants
            await Clients.Group($"Conversation_{conversationId}")
                .SendAsync("ConversationCleared", new
                {
                    ConversationId = conversationId,
                    ClearedAt = DateTime.UtcNow
                });

            _logger.LogInformation("Conversation history cleared for {ConversationId} by connection {ConnectionId}", 
                conversationId, Context.ConnectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing conversation history {ConversationId} for connection {ConnectionId}", 
                conversationId, Context.ConnectionId);
            
            await Clients.Caller.SendAsync("Error", new
            {
                Type = "ClearHistoryError",
                Message = "Không thể xóa lịch sử cuộc trò chuyện.",
                ConversationId = conversationId
            });
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Ping để kiểm tra kết nối
    /// </summary>
    public async Task Ping()
    {
        try
        {
            await Clients.Caller.SendAsync("Pong", new
            {
                ConnectionId = Context.ConnectionId,
                Timestamp = DateTime.UtcNow,
                Message = "Connection is alive"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Ping for connection {ConnectionId}", Context.ConnectionId);
        }
    }

    /// <summary>
    /// Lấy thông tin kết nối hiện tại
    /// </summary>
    public async Task GetConnectionInfo()
    {
        try
        {
            var connection = await _chatHubService.GetConnectionAsync(Context.ConnectionId);
            await Clients.Caller.SendAsync("ConnectionInfo", connection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting connection info for {ConnectionId}", Context.ConnectionId);
        }
    }

    /// <summary>
    /// Cập nhật trạng thái người dùng
    /// </summary>
    /// <param name="status">Trạng thái mới</param>
    public async Task UpdateUserStatus(string status)
    {
        try
        {
            var connection = await _chatHubService.GetConnectionAsync(Context.ConnectionId);
            if (connection != null)
            {
                // Add status to metadata
                connection.IsOnline = status == "online";
                await _chatHubService.UpdateConnectionAsync(connection);

                // Notify contacts about status change
                var onlineUsers = await _chatHubService.GetOnlineUsersInConversationAsync(
                    connection.CurrentConversationId ?? "");
                
                foreach (var user in onlineUsers.Where(u => u.ConnectionId != Context.ConnectionId))
                {
                    await Clients.Client(user.ConnectionId).SendAsync("UserStatusChanged", new
                    {
                        UserId = connection.UserId,
                        UserName = connection.UserName,
                        Status = status,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user status for connection {ConnectionId}", Context.ConnectionId);
        }
    }

    #endregion
} 