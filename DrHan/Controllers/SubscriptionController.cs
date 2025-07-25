using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using DrHan.Application.Services.SubscriptionServices.Commands.CreateSubscription;
using DrHan.Application.Services.SubscriptionServices.Commands.CancelSubscription;
using DrHan.Application.Services.SubscriptionServices.Commands.RenewSubscription;
using DrHan.Application.Services.SubscriptionServices.Commands.UpgradeSubscription;
using DrHan.Application.Services.SubscriptionServices.Queries.GetSubscriptionStatus;
using DrHan.Application.Services.SubscriptionServices.Queries.GetPurchaseHistory;
using DrHan.Application.Services.SubscriptionServices.Queries.GetUsageHistory;
using DrHan.Application.Services.SubscriptionServices.Queries.GetSubscriptionHistory;
using DrHan.Application.DTOs.Subscription;
using DrHan.Application.Interfaces.Services.AuthenticationServices;
using DrHan.Application.Interfaces.Services;
using AutoMapper;
using DrHan.Application.Commons;
using DrHan.Application.Interfaces.Repository;
using DrHan.Domain.Entities.Users;
using DrHan.Domain.Constants.Roles;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace DrHan.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserSubscriptionController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<UserSubscriptionController> _logger;
    private readonly IUserContext _userContext;
    private readonly ISubscriptionService _subscriptionService;

    public UserSubscriptionController(
        IMediator mediator, 
        ILogger<UserSubscriptionController> logger, 
        IUserContext userContext,
        ISubscriptionService subscriptionService)
    {
        _mediator = mediator;
        _logger = logger;
        _userContext = userContext;
        _subscriptionService = subscriptionService;
    }

    /// <summary>
    /// Get current user's subscription status
    /// </summary>
    [HttpGet("status")]
    [Authorize]
    public async Task<IActionResult> GetSubscriptionStatus()
    {
        try
        {
            var userId = _userContext.GetCurrentUserId();
            if (userId == null)
                return Unauthorized("User ID not found in token");

            var query = new GetSubscriptionStatusQuery { UserId = userId.Value };
            var response = await _mediator.Send(query);
            
            if (!response.IsSucceeded)
                return BadRequest(response);
                
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving subscription status");
            return StatusCode(500, "An error occurred while retrieving subscription status");
        }
    }

    /// <summary>
    /// Create a new subscription for current user
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateSubscription([FromBody] CreateSubscriptionRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = _userContext.GetCurrentUserId();

            var command = new CreateSubscriptionCommand 
            { 
                UserId = userId.Value,
                PlanId = request.PlanId
            };
            
            var response = await _mediator.Send(command);
            
            if (!response.IsSucceeded)
                return BadRequest(response);
                
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating subscription");
            return StatusCode(500, "An error occurred while creating the subscription");
        }
    }

    /// <summary>
    /// Cancel current user's subscription
    /// </summary>
    [HttpPost("cancel")]
    [Authorize]
    public async Task<IActionResult> CancelSubscription([FromBody] CancelSubscriptionRequestDto? request = null)
    {
        try
        {
            var userId = _userContext.GetCurrentUserId();
            if (userId == null)
                return Unauthorized("User ID not found in token");

            var command = new CancelSubscriptionCommand 
            { 
                UserId = userId.Value,
                CancellationReason = request?.CancellationReason
            };
            
            var response = await _mediator.Send(command);
            
            if (!response.IsSucceeded)
                return BadRequest(response);
                
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling subscription");
            return StatusCode(500, "An error occurred while cancelling the subscription");
        }
    }

    /// <summary>
    /// Renew current user's subscription
    /// </summary>
    [HttpPost("renew")]
    [Authorize]
    public async Task<IActionResult> RenewSubscription()
    {
        try
        {
            var userId = _userContext.GetCurrentUserId();
            if (userId == null)
                return Unauthorized("User ID not found in token");

            var command = new RenewSubscriptionCommand { UserId = userId.Value };
            var response = await _mediator.Send(command);
            
            if (!response.IsSucceeded)
                return BadRequest(response);
                
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error renewing subscription");
            return StatusCode(500, "An error occurred while renewing the subscription");
        }
    }

    /// <summary>
    /// Upgrade current user's subscription to a higher plan
    /// </summary>
    [HttpPost("upgrade")]
    [Authorize]
    public async Task<IActionResult> UpgradeSubscription([FromBody] UpgradeSubscriptionRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = _userContext.GetCurrentUserId();
            if (userId == null)
                return Unauthorized("User ID not found in token");

            var command = new UpgradeSubscriptionCommand 
            { 
                UserId = userId.Value,
                NewPlanId = request.NewPlanId
            };
            
            var response = await _mediator.Send(command);
            
            if (!response.IsSucceeded)
                return BadRequest(response);
                
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upgrading subscription");
            return StatusCode(500, "An error occurred while upgrading the subscription");
        }
    }

    /// <summary>
    /// Check if current user can use a specific feature
    /// </summary>
    [HttpGet("can-use/{featureName}")]
    [Authorize]
    public async Task<IActionResult> CanUseFeature(string featureName, [FromQuery] string limitType = "daily")
    {
        try
        {
            var userId = _userContext.GetCurrentUserId();
            if (userId == null)
                return Unauthorized("User ID not found in token");

            var canUse = await _subscriptionService.CanUseFeature(userId.Value, featureName, limitType);
            
            return Ok(new { CanUse = canUse, FeatureName = featureName, LimitType = limitType });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking feature access for {FeatureName}", featureName);
            return StatusCode(500, "An error occurred while checking feature access");
        }
    }

    /// <summary>
    /// Track usage of a feature for current user
    /// </summary>
    [HttpPost("track-usage")]
    [Authorize]
    public async Task<IActionResult> TrackUsage([FromBody] TrackUsageRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = _userContext.GetCurrentUserId();
            if (userId == null)
                return Unauthorized("User ID not found in token");

            await _subscriptionService.TrackUsage(userId.Value, request.FeatureName, request.Count);
            
            return Ok(new { Success = true, Message = "Usage tracked successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking usage for feature {FeatureName}", request?.FeatureName);
            return StatusCode(500, "An error occurred while tracking usage");
        }
    }

    /// <summary>
    /// Get usage count for a feature
    /// </summary>
    [HttpGet("usage/{featureName}")]
    [Authorize]
    public async Task<IActionResult> GetUsageCount(string featureName, [FromQuery] DateTime? fromDate = null)
    {
        try
        {
            var userId = _userContext.GetCurrentUserId();
            if (userId == null)
                return Unauthorized("User ID not found in token");

            var usageCount = await _subscriptionService.GetUsageCount(userId.Value, featureName, fromDate);
            
            return Ok(new 
            { 
                FeatureName = featureName, 
                UsageCount = usageCount,
                FromDate = fromDate ?? DateTime.Now.Date
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting usage count for feature {FeatureName}", featureName);
            return StatusCode(500, "An error occurred while retrieving usage count");
        }
    }

    /// <summary>
    /// Get current user's purchase history
    /// </summary>
    [HttpGet("history/purchases")]
    [Authorize]
    public async Task<IActionResult> GetPurchaseHistory([FromQuery] HistoryFilterDto filter)
    {
        try
        {
            var userId = _userContext.GetCurrentUserId();
            if (userId == null)
                return Unauthorized("User ID not found in token");

            var query = new GetPurchaseHistoryQuery 
            { 
                UserId = userId.Value,
                FromDate = filter.FromDate,
                ToDate = filter.ToDate,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };
            
            var response = await _mediator.Send(query);
            
            if (!response.IsSucceeded)
                return BadRequest(response);
                
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving purchase history");
            return StatusCode(500, "An error occurred while retrieving purchase history");
        }
    }

    /// <summary>
    /// Get current user's usage history
    /// </summary>
    [HttpGet("history/usage")]
    [Authorize]
    public async Task<IActionResult> GetUsageHistory([FromQuery] HistoryFilterDto filter, [FromQuery] string? featureType = null)
    {
        try
        {
            var userId = _userContext.GetCurrentUserId();
            if (userId == null)
                return Unauthorized("User ID not found in token");

            var query = new GetUsageHistoryQuery 
            { 
                UserId = userId.Value,
                FeatureType = featureType,
                FromDate = filter.FromDate,
                ToDate = filter.ToDate,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };
            
            var response = await _mediator.Send(query);
            
            if (!response.IsSucceeded)
                return BadRequest(response);
                
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving usage history");
            return StatusCode(500, "An error occurred while retrieving usage history");
        }
    }

    /// <summary>
    /// Get current user's subscription history
    /// </summary>
    [HttpGet("history/subscriptions")]
    [Authorize]
    public async Task<IActionResult> GetSubscriptionHistory([FromQuery] HistoryFilterDto filter)
    {
        try
        {
            var userId = _userContext.GetCurrentUserId();
            if (userId == null)
                return Unauthorized("User ID not found in token");

            var query = new GetSubscriptionHistoryQuery 
            { 
                UserId = userId.Value,
                FromDate = filter.FromDate,
                ToDate = filter.ToDate,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };
            
            var response = await _mediator.Send(query);
            
            if (!response.IsSucceeded)
                return BadRequest(response);
                
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving subscription history");
            return StatusCode(500, "An error occurred while retrieving subscription history");
        }
    }
}

// Additional DTOs for the controller
public class CancelSubscriptionRequestDto
{
    public string? CancellationReason { get; set; }
}

public class TrackUsageRequestDto
{
    public string FeatureName { get; set; } = string.Empty;
    public int Count { get; set; } = 1;
}

