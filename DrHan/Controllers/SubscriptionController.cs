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
                FromDate = fromDate ?? DateTime.UtcNow.Date
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

[ApiController]
[Route("api/[controller]")]
public class SubscriptionController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<SubscriptionController> _logger;

    public SubscriptionController(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<SubscriptionController> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    #region Public Endpoints - View Plans

    /// <summary>
    /// Get all active subscription plans with features (Public)
    /// </summary>
    [HttpGet("plans")]
    [AllowAnonymous]
    public async Task<ActionResult<AppResponse<IEnumerable<SubscriptionPlanDto>>>> GetAllPlans()
    {
        try
        {
            var plans = await _unitOfWork.Repository<SubscriptionPlan>()
                .ListAsync(
                    filter: p => p.IsActive,
                    includeProperties: q => q.Include(p => p.PlanFeatures),
                    orderBy: q => q.OrderBy(p => p.Price));

            var planDtos = _mapper.Map<IEnumerable<SubscriptionPlanDto>>(plans);

            return Ok(new AppResponse<IEnumerable<SubscriptionPlanDto>>()
                .SetSuccessResponse(planDtos, "success", "Subscription plans retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription plans");
            return StatusCode(500, new AppResponse<IEnumerable<SubscriptionPlanDto>>()
                .SetErrorResponse("error", "Failed to retrieve subscription plans"));
        }
    }

    /// <summary>
    /// Get specific subscription plan by ID with features (Public)
    /// </summary>
    [HttpGet("plans/{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<AppResponse<SubscriptionPlanDto>>> GetPlanById(int id)
    {
        try
        {
            var plans = await _unitOfWork.Repository<SubscriptionPlan>()
                .ListAsync(
                    filter: p => p.Id == id && p.IsActive,
                    includeProperties: q => q.Include(p => p.PlanFeatures));

            var plan = plans.FirstOrDefault();

            if (plan == null)
            {
                return NotFound(new AppResponse<SubscriptionPlanDto>()
                    .SetErrorResponse("not_found", "Subscription plan not found"));
            }

            var planDto = _mapper.Map<SubscriptionPlanDto>(plan);

            return Ok(new AppResponse<SubscriptionPlanDto>()
                .SetSuccessResponse(planDto, "success", "Subscription plan retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription plan {PlanId}", id);
            return StatusCode(500, new AppResponse<SubscriptionPlanDto>()
                .SetErrorResponse("error", "Failed to retrieve subscription plan"));
        }
    }

    #endregion

    #region Admin Endpoints - Manage Plans

    /// <summary>
    /// Get all subscription plans (including inactive) for admin management
    /// </summary>
    [HttpGet("admin/plans")]
    [Authorize(Roles = UserRoles.Admin)]
    public async Task<ActionResult<AppResponse<IEnumerable<SubscriptionPlanDto>>>> GetAllPlansAdmin()
    {
        try
        {
            var plans = await _unitOfWork.Repository<SubscriptionPlan>()
                .ListAsync(
                    includeProperties: q => q.Include(p => p.PlanFeatures),
                    orderBy: q => q.OrderBy(p => p.CreatedAt));

            var planDtos = _mapper.Map<IEnumerable<SubscriptionPlanDto>>(plans);

            return Ok(new AppResponse<IEnumerable<SubscriptionPlanDto>>()
                .SetSuccessResponse(planDtos, "success", "All subscription plans retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all subscription plans for admin");
            return StatusCode(500, new AppResponse<IEnumerable<SubscriptionPlanDto>>()
                .SetErrorResponse("error", "Failed to retrieve subscription plans"));
        }
    }

    /// <summary>
    /// Create new subscription plan (Admin only)
    /// </summary>
    [HttpPost("admin/plans")]
    [Authorize(Roles = UserRoles.Admin)]
    public async Task<ActionResult<AppResponse<SubscriptionPlanDto>>> CreatePlan([FromBody] CreateSubscriptionPlanDto request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new AppResponse<SubscriptionPlanDto>().SetErrorResponse("validation", "Invalid input data"));

            var plan = _mapper.Map<SubscriptionPlan>(request);
            
            await _unitOfWork.Repository<SubscriptionPlan>().AddAsync(plan);
            await _unitOfWork.CompleteAsync();

            // Add features if provided
            if (request.Features.Any())
            {
                var features = request.Features.Select(f => new PlanFeature
                {
                    PlanId = plan.Id,
                    FeatureName = f.FeatureName,
                    Description = f.Description,
                    IsEnabled = f.IsEnabled,
                    CreatedAt = DateTime.UtcNow
                }).ToList();

                await _unitOfWork.Repository<PlanFeature>().AddRangeAsync(features);
                await _unitOfWork.CompleteAsync();

                plan.PlanFeatures = features;
            }

            var planDto = _mapper.Map<SubscriptionPlanDto>(plan);

            _logger.LogInformation("Subscription plan created successfully: {PlanName} (ID: {PlanId})", plan.Name, plan.Id);
            
            return CreatedAtAction(nameof(GetPlanById), new { id = plan.Id }, 
                new AppResponse<SubscriptionPlanDto>()
                    .SetSuccessResponse(planDto, "success", "Subscription plan created successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating subscription plan");
            return StatusCode(500, new AppResponse<SubscriptionPlanDto>()
                .SetErrorResponse("error", "Failed to create subscription plan"));
        }
    }

    /// <summary>
    /// Update subscription plan (Admin only)
    /// </summary>
    [HttpPut("admin/plans/{id}")]
    [Authorize(Roles = UserRoles.Admin)]
    public async Task<ActionResult<AppResponse<SubscriptionPlanDto>>> UpdatePlan(int id, [FromBody] UpdateSubscriptionPlanDto request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new AppResponse<SubscriptionPlanDto>().SetErrorResponse("validation", "Invalid input data"));

            var plan = await _unitOfWork.Repository<SubscriptionPlan>().FindAsync(p => p.Id == id);

            if (plan == null)
            {
                return NotFound(new AppResponse<SubscriptionPlanDto>()
                    .SetErrorResponse("not_found", "Subscription plan not found"));
            }

            _mapper.Map(request, plan);
            
            _unitOfWork.Repository<SubscriptionPlan>().Update(plan);
            await _unitOfWork.CompleteAsync();

            var planDto = _mapper.Map<SubscriptionPlanDto>(plan);

            _logger.LogInformation("Subscription plan updated successfully: {PlanName} (ID: {PlanId})", plan.Name, plan.Id);

            return Ok(new AppResponse<SubscriptionPlanDto>()
                .SetSuccessResponse(planDto, "success", "Subscription plan updated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating subscription plan {PlanId}", id);
            return StatusCode(500, new AppResponse<SubscriptionPlanDto>()
                .SetErrorResponse("error", "Failed to update subscription plan"));
        }
    }

    /// <summary>
    /// Delete/Deactivate subscription plan (Admin only)
    /// </summary>
    [HttpDelete("admin/plans/{id}")]
    [Authorize(Roles = UserRoles.Admin)]
    public async Task<ActionResult<AppResponse<bool>>> DeletePlan(int id)
    {
        try
        {
            var plan = await _unitOfWork.Repository<SubscriptionPlan>().FindAsync(p => p.Id == id);

            if (plan == null)
            {
                return NotFound(new AppResponse<bool>()
                    .SetErrorResponse("not_found", "Subscription plan not found"));
            }

            // Check if plan has active subscriptions
            var activeSubscriptions = await _unitOfWork.Repository<UserSubscription>()
                .ListAsync(filter: s => s.PlanId == id && s.Status == Domain.Constants.Status.UserSubscriptionStatus.Active);
            var activeSubscriptionCount = activeSubscriptions.Count;

            if (activeSubscriptionCount > 0)
            {
                // Deactivate instead of delete if there are active subscriptions
                plan.IsActive = false;
                _unitOfWork.Repository<SubscriptionPlan>().Update(plan);
                await _unitOfWork.CompleteAsync();

                _logger.LogInformation("Subscription plan deactivated due to active subscriptions: {PlanName} (ID: {PlanId})", plan.Name, plan.Id);

                return Ok(new AppResponse<bool>()
                    .SetSuccessResponse(true, "success", "Subscription plan deactivated (has active subscriptions)"));
            }
            else
            {
                // Safe to delete if no active subscriptions
                _unitOfWork.Repository<SubscriptionPlan>().Delete(plan);
                await _unitOfWork.CompleteAsync();

                _logger.LogInformation("Subscription plan deleted: {PlanName} (ID: {PlanId})", plan.Name, plan.Id);

                return Ok(new AppResponse<bool>()
                    .SetSuccessResponse(true, "success", "Subscription plan deleted successfully"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting subscription plan {PlanId}", id);
            return StatusCode(500, new AppResponse<bool>()
                .SetErrorResponse("error", "Failed to delete subscription plan"));
        }
    }

    #endregion

    #region Admin Endpoints - Manage Features

    /// <summary>
    /// Add feature to subscription plan (Admin only)
    /// </summary>
    [HttpPost("admin/plans/{planId}/features")]
    [Authorize(Roles = UserRoles.Admin)]
    public async Task<ActionResult<AppResponse<PlanFeatureDto>>> AddFeatureToPlan(int planId, [FromBody] CreatePlanFeatureDto request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new AppResponse<PlanFeatureDto>().SetErrorResponse("validation", "Invalid input data"));

            var plan = await _unitOfWork.Repository<SubscriptionPlan>().FindAsync(p => p.Id == planId);

            if (plan == null)
            {
                return NotFound(new AppResponse<PlanFeatureDto>()
                    .SetErrorResponse("not_found", "Subscription plan not found"));
            }

            var feature = _mapper.Map<PlanFeature>(request);
            feature.PlanId = planId;

            await _unitOfWork.Repository<PlanFeature>().AddAsync(feature);
            await _unitOfWork.CompleteAsync();

            var featureDto = _mapper.Map<PlanFeatureDto>(feature);

            _logger.LogInformation("Feature added to plan: {FeatureName} to {PlanName} (ID: {PlanId})", feature.FeatureName, plan.Name, planId);

            return Ok(new AppResponse<PlanFeatureDto>()
                .SetSuccessResponse(featureDto, "success", "Feature added to plan successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding feature to plan {PlanId}", planId);
            return StatusCode(500, new AppResponse<PlanFeatureDto>()
                .SetErrorResponse("error", "Failed to add feature to plan"));
        }
    }

    /// <summary>
    /// Remove feature from subscription plan (Admin only)
    /// </summary>
    [HttpDelete("admin/plans/{planId}/features/{featureId}")]
    [Authorize(Roles = UserRoles.Admin)]
    public async Task<ActionResult<AppResponse<bool>>> RemoveFeatureFromPlan(int planId, int featureId)
    {
        try
        {
            var feature = await _unitOfWork.Repository<PlanFeature>()
                .FindAsync(f => f.Id == featureId && f.PlanId == planId);

            if (feature == null)
            {
                return NotFound(new AppResponse<bool>()
                    .SetErrorResponse("not_found", "Plan feature not found"));
            }

            _unitOfWork.Repository<PlanFeature>().Delete(feature);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Feature removed from plan: {FeatureName} (ID: {FeatureId}) from plan {PlanId}", feature.FeatureName, featureId, planId);

            return Ok(new AppResponse<bool>()
                .SetSuccessResponse(true, "success", "Feature removed from plan successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing feature {FeatureId} from plan {PlanId}", featureId, planId);
            return StatusCode(500, new AppResponse<bool>()
                .SetErrorResponse("error", "Failed to remove feature from plan"));
        }
    }

    #endregion

    #region Admin Endpoints - View User Subscriptions

    /// <summary>
    /// Get all user subscriptions for admin management
    /// </summary>
    [HttpGet("admin/user-subscriptions")]
    [Authorize(Roles = UserRoles.Admin)]
    public async Task<ActionResult<AppResponse<IEnumerable<AdminUserSubscriptionDto>>>> GetAllUserSubscriptions(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        [FromQuery] int? planId = null)
    {
        try
        {
            // Build filter expression
            Expression<Func<UserSubscription, bool>>? filter = null;
            
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<Domain.Constants.Status.UserSubscriptionStatus>(status, out var statusEnum))
            {
                if (planId.HasValue)
                {
                    filter = s => s.Status == statusEnum && s.PlanId == planId.Value;
                }
                else
                {
                    filter = s => s.Status == statusEnum;
                }
            }
            else if (planId.HasValue)
            {
                filter = s => s.PlanId == planId.Value;
            }

            // Get all subscriptions with filtering
            var allSubscriptions = await _unitOfWork.Repository<UserSubscription>()
                .ListAsync(
                    filter: filter,
                    includeProperties: q => q.Include(s => s.ApplicationUser).Include(s => s.Plan),
                    orderBy: q => q.OrderByDescending(s => s.CreatedAt));

            var totalCount = allSubscriptions.Count;

            // Apply pagination
            var subscriptions = allSubscriptions
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var subscriptionDtos = _mapper.Map<IEnumerable<AdminUserSubscriptionDto>>(subscriptions);

            var response = new
            {
                Data = subscriptionDtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            };

            return Ok(new AppResponse<object>()
                .SetSuccessResponse(response, "success", "User subscriptions retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user subscriptions for admin");
            return StatusCode(500, new AppResponse<IEnumerable<AdminUserSubscriptionDto>>()
                .SetErrorResponse("error", "Failed to retrieve user subscriptions"));
        }
    }

    #endregion
} 