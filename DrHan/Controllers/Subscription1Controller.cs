using AutoMapper;
using DrHan.Application.Commons;
using DrHan.Application.DTOs.Subscription;
using DrHan.Application.Interfaces.Repository;
using DrHan.Domain.Constants.Roles;
using DrHan.Domain.Entities.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace DrHan.API.Controllers
{
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
                        CreatedAt = DateTime.Now
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
}
