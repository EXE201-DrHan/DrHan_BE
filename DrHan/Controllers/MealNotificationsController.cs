using DrHan.Application.Commons;
using DrHan.Application.DTOs.Notifications;
using DrHan.Application.Interfaces.Services;
using DrHan.Application.Interfaces.Services.AuthenticationServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DrHan.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Requires authentication
public class MealNotificationsController : ControllerBase
{
    private readonly IMealNotificationService _mealNotificationService;
    private readonly IUserContext _userContext;
    private readonly ILogger<MealNotificationsController> _logger;

    public MealNotificationsController(
        IMealNotificationService mealNotificationService,
        IUserContext userContext,
        ILogger<MealNotificationsController> logger)
    {
        _mealNotificationService = mealNotificationService;
        _userContext = userContext;
        _logger = logger;
    }

    /// <summary>
    /// 🔔 Get user's meal notification settings
    /// </summary>
    /// <returns>Current notification settings</returns>
    [HttpGet("settings")]
    [ProducesResponseType(typeof(AppResponse<UserMealNotificationSettingsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<AppResponse<UserMealNotificationSettingsDto>>> GetNotificationSettings()
    {
        try
        {
            var userId = _userContext.GetCurrentUserId().GetValueOrDefault();
            var result = await _mealNotificationService.GetUserNotificationSettingsAsync(userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification settings");
            var errorResponse = new AppResponse<UserMealNotificationSettingsDto>()
                .SetErrorResponse("Error", "Failed to retrieve notification settings");
            return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
        }
    }

    /// <summary>
    /// ⚙️ Update user's meal notification settings
    /// </summary>
    /// <param name="updateDto">Updated notification settings</param>
    /// <returns>Updated settings</returns>
    [HttpPut("settings")]
    [ProducesResponseType(typeof(AppResponse<UserMealNotificationSettingsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AppResponse<UserMealNotificationSettingsDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AppResponse<UserMealNotificationSettingsDto>>> UpdateNotificationSettings(
        [FromBody] UpdateMealNotificationSettingsDto updateDto)
    {
        try
        {
            var userId = _userContext.GetCurrentUserId().GetValueOrDefault();
            var result = await _mealNotificationService.UpdateNotificationSettingsAsync(userId, updateDto);
            
            if (result.IsSucceeded)
                return Ok(result);
            
            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating notification settings");
            var errorResponse = new AppResponse<UserMealNotificationSettingsDto>()
                .SetErrorResponse("Error", "Failed to update notification settings");
            return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
        }
    }

    /// <summary>
    /// 📜 Get notification history
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20)</param>
    /// <returns>List of notification logs</returns>
    [HttpGet("history")]
    [ProducesResponseType(typeof(AppResponse<List<MealNotificationLogDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<AppResponse<List<MealNotificationLogDto>>>> GetNotificationHistory(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var userId = _userContext.GetCurrentUserId().GetValueOrDefault();
            var result = await _mealNotificationService.GetNotificationHistoryAsync(userId, pageNumber, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification history");
            var errorResponse = new AppResponse<List<MealNotificationLogDto>>()
                .SetErrorResponse("Error", "Failed to retrieve notification history");
            return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
        }
    }

    /// <summary>
    /// 👀 Preview today's notifications
    /// </summary>
    /// <returns>List of notifications that would be sent today</returns>
    [HttpGet("preview")]
    [ProducesResponseType(typeof(AppResponse<List<NotificationPreviewDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<AppResponse<List<NotificationPreviewDto>>>> PreviewTodaysNotifications()
    {
        try
        {
            var userId = _userContext.GetCurrentUserId().GetValueOrDefault();
            var result = await _mealNotificationService.PreviewTodaysNotificationsAsync(userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating notification preview");
            var errorResponse = new AppResponse<List<NotificationPreviewDto>>()
                .SetErrorResponse("Error", "Failed to generate notification preview");
            return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
        }
    }

    /// <summary>
    /// 🧪 Send test notification
    /// </summary>
    /// <returns>Success status</returns>
    [HttpPost("test")]
    [ProducesResponseType(typeof(AppResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<AppResponse<bool>>> SendTestNotification()
    {
        try
        {
            var userId = _userContext.GetCurrentUserId().GetValueOrDefault();
            var result = await _mealNotificationService.SendTestNotificationAsync(userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending test notification");
            var errorResponse = new AppResponse<bool>()
                .SetErrorResponse("Error", "Failed to send test notification");
            return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
        }
    }

    /// <summary>
    /// 🔧 Trigger notification processing manually (for testing)
    /// </summary>
    /// <returns>Success status</returns>
    [HttpPost("process")]
    [ProducesResponseType(typeof(AppResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<AppResponse<bool>>> TriggerNotificationProcessing()
    {
        try
        {
            await _mealNotificationService.ProcessMealNotificationsAsync();
            
            var response = new AppResponse<bool>()
                .SetSuccessResponse(true, "Success", "Notification processing triggered successfully");
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering notification processing");
            var errorResponse = new AppResponse<bool>()
                .SetErrorResponse("Error", "Failed to trigger notification processing");
            return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
        }
    }

    /// <summary>
    /// 🌍 Get available timezones
    /// </summary>
    /// <returns>List of available timezone IDs</returns>
    [HttpGet("timezones")]
    [ProducesResponseType(typeof(AppResponse<List<string>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<AppResponse<List<string>>>> GetAvailableTimeZones()
    {
        try
        {
            var result = await _mealNotificationService.GetAvailableTimeZonesAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available timezones");
            var errorResponse = new AppResponse<List<string>>()
                .SetErrorResponse("Error", "Failed to retrieve available timezones");
            return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
        }
    }
} 