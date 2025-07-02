using DrHan.Application.Commons;
using DrHan.Application.DTOs.Chatbot;
using DrHan.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace DrHan.Controllers;

[ApiController]
[Route("api/[controller]")]
[Tags("Chatbot")]
public class ChatbotController : ControllerBase
{
    private readonly IChatbotService _chatbotService;
    private readonly ILogger<ChatbotController> _logger;

    public ChatbotController(IChatbotService chatbotService, ILogger<ChatbotController> logger)
    {
        _chatbotService = chatbotService ?? throw new ArgumentNullException(nameof(chatbotService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Xử lý tin nhắn chat từ người dùng
    /// </summary>
    /// <param name="request">Yêu cầu chat từ người dùng</param>
    /// <returns>Phản hồi từ AI chatbot</returns>
    [HttpPost("chat")]
    [ProducesResponseType(typeof(AppResponse<ChatbotResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AppResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(AppResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Chat([FromBody] ChatbotRequestDto request)
    {
        try
        {
            _logger.LogInformation("Received chat request: {Message}", request.Message);

            if (!ModelState.IsValid)
            {
                var errors = ModelState.SelectMany(x => x.Value!.Errors).Select(x => x.ErrorMessage).ToArray();
                return BadRequest(new AppResponse<object>().SetErrorResponse("ValidationErrors", errors));
            }

            var response = await _chatbotService.ProcessMessageAsync(request);

            _logger.LogInformation("Successfully processed chat request for conversation: {ConversationId}", 
                response.ConversationId);

            return Ok(new AppResponse<ChatbotResponseDto>().SetSuccessResponse(response, "Message", "Tin nhắn đã được xử lý thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat request: {Message}", request?.Message);
            return StatusCode(500, new AppResponse<object>().SetErrorResponse("Error", 
                "Đã xảy ra lỗi khi xử lý tin nhắn. Vui lòng thử lại sau."));
        }
    }

    /// <summary>
    /// Lấy lịch sử cuộc trò chuyện
    /// </summary>
    /// <param name="conversationId">ID cuộc trò chuyện</param>
    /// <param name="limit">Số lượng tin nhắn tối đa (mặc định: 20)</param>
    /// <returns>Lịch sử cuộc trò chuyện</returns>
    [HttpGet("conversation/{conversationId}/history")]
    [ProducesResponseType(typeof(AppResponse<List<ChatMessageDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AppResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(AppResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetConversationHistory(
        [FromRoute] string conversationId,
        [FromQuery] int limit = 20)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(conversationId))
            {
                return BadRequest(new AppResponse<object>().SetErrorResponse("Error", "ID cuộc trò chuyện không hợp lệ"));
            }

            if (limit <= 0 || limit > 100)
            {
                return BadRequest(new AppResponse<object>().SetErrorResponse("Error", "Limit phải từ 1 đến 100"));
            }

            var history = await _chatbotService.GetConversationHistoryAsync(conversationId, limit);

            return Ok(new AppResponse<List<ChatMessageDto>>().SetSuccessResponse(history, "Message", 
                $"Lấy lịch sử cuộc trò chuyện thành công ({history.Count} tin nhắn)"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversation history: {ConversationId}", conversationId);
            return StatusCode(500, new AppResponse<object>().SetErrorResponse("Error", 
                "Đã xảy ra lỗi khi lấy lịch sử cuộc trò chuyện"));
        }
    }

    /// <summary>
    /// Xóa lịch sử cuộc trò chuyện
    /// </summary>
    /// <param name="conversationId">ID cuộc trò chuyện</param>
    /// <returns>Kết quả xóa</returns>
    [HttpDelete("conversation/{conversationId}")]
    [ProducesResponseType(typeof(AppResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AppResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(AppResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ClearConversation([FromRoute] string conversationId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(conversationId))
            {
                return BadRequest(new AppResponse<object>().SetErrorResponse("Error", "ID cuộc trò chuyện không hợp lệ"));
            }

            await _chatbotService.ClearConversationAsync(conversationId);

            return Ok(new AppResponse<object>().SetSuccessResponse(null, "Message", "Đã xóa lịch sử cuộc trò chuyện thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing conversation: {ConversationId}", conversationId);
            return StatusCode(500, new AppResponse<object>().SetErrorResponse("Error", 
                "Đã xảy ra lỗi khi xóa lịch sử cuộc trò chuyện"));
        }
    }

    /// <summary>
    /// Lấy danh sách câu hỏi gợi ý theo danh mục
    /// </summary>
    /// <param name="category">Danh mục: general, allergy, mealplan, app_help</param>
    /// <param name="language">Ngôn ngữ (mặc định: vi)</param>
    /// <returns>Danh sách câu hỏi gợi ý</returns>
    [HttpGet("suggestions")]
    [ProducesResponseType(typeof(AppResponse<List<string>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AppResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(AppResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSuggestedQuestions(
        [FromQuery] string category = "general",
        [FromQuery] string language = "vi")
    {
        try
        {
            var validCategories = new[] { "general", "allergy", "mealplan", "app_help" };
            if (!validCategories.Contains(category.ToLower()))
            {
                return BadRequest(new AppResponse<object>().SetErrorResponse("Error", 
                    $"Danh mục không hợp lệ. Các danh mục hợp lệ: {string.Join(", ", validCategories)}"));
            }

            var suggestions = await _chatbotService.GetSuggestedQuestionsAsync(category.ToLower(), language);

            return Ok(new AppResponse<List<string>>().SetSuccessResponse(suggestions, "Message", 
                $"Lấy danh sách câu hỏi gợi ý cho danh mục '{category}' thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting suggested questions for category: {Category}", category);
            return StatusCode(500, new AppResponse<object>().SetErrorResponse("Error", 
                "Đã xảy ra lỗi khi lấy danh sách câu hỏi gợi ý"));
        }
    }

    /// <summary>
    /// Phân tích intent (mục đích) của tin nhắn
    /// </summary>
    /// <param name="message">Tin nhắn cần phân tích</param>
    /// <param name="language">Ngôn ngữ (mặc định: vi)</param>
    /// <returns>Kết quả phân tích intent</returns>
    [HttpPost("analyze-intent")]
    [ProducesResponseType(typeof(AppResponse<IntentAnalysisResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AppResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(AppResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AnalyzeIntent([FromBody] IntentAnalysisRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new AppResponse<object>().SetErrorResponse("Error", "Tin nhắn không được để trống"));
            }

            if (request.Message.Length > 1000)
            {
                return BadRequest(new AppResponse<object>().SetErrorResponse("Error", "Tin nhắn không được vượt quá 1000 ký tự"));
            }

            var (category, confidence) = await _chatbotService.AnalyzeIntentAsync(request.Message, request.Language ?? "vi");

            var result = new IntentAnalysisResult
            {
                Category = category,
                Confidence = confidence,
                Message = request.Message
            };

            return Ok(new AppResponse<IntentAnalysisResult>().SetSuccessResponse(result, "Message", "Phân tích intent thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing intent for message: {Message}", request?.Message);
            return StatusCode(500, new AppResponse<object>().SetErrorResponse("Error", "Đã xảy ra lỗi khi phân tích intent"));
        }
    }

    /// <summary>
    /// Lấy thông tin trạng thái chatbot
    /// </summary>
    /// <returns>Thông tin trạng thái</returns>
    [HttpGet("status")]
    [ProducesResponseType(typeof(AppResponse<ChatbotStatusDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetChatbotStatus()
    {
        try
        {
            var status = new ChatbotStatusDto
            {
                IsOnline = true,
                Version = "1.0.0",
                SupportedLanguages = new[] { "vi", "en" },
                SupportedCategories = new[] { "general", "allergy", "mealplan", "app_help" },
                LastUpdated = DateTime.UtcNow
            };

            return Ok(new AppResponse<ChatbotStatusDto>().SetSuccessResponse(status, "Message", "Lấy trạng thái chatbot thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chatbot status");
            return StatusCode(500, new AppResponse<object>().SetErrorResponse("Error", "Đã xảy ra lỗi khi lấy trạng thái chatbot"));
        }
    }
}

public class IntentAnalysisRequest
{
    [Required(ErrorMessage = "Tin nhắn không được để trống")]
    [StringLength(1000, ErrorMessage = "Tin nhắn không được vượt quá 1000 ký tự")]
    public string Message { get; set; } = string.Empty;

    public string? Language { get; set; } = "vi";
}

public class IntentAnalysisResult
{
    public string Category { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class ChatbotStatusDto
{
    public bool IsOnline { get; set; }
    public string Version { get; set; } = string.Empty;
    public string[] SupportedLanguages { get; set; } = Array.Empty<string>();
    public string[] SupportedCategories { get; set; } = Array.Empty<string>();
    public DateTime LastUpdated { get; set; }
} 