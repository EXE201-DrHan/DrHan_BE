using MediatR;
using AutoMapper;
using DrHan.Application.Commons;
using DrHan.Application.DTOs.Subscription;
using DrHan.Application.Interfaces.Repository;
using DrHan.Application.Interfaces.Services;
using DrHan.Domain.Entities.Users;
using DrHan.Domain.Constants.Status;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace DrHan.Application.Services.SubscriptionServices.Queries.GetSubscriptionStatus;

public class GetSubscriptionStatusQueryHandler : IRequestHandler<GetSubscriptionStatusQuery, AppResponse<SubscriptionStatusDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ISubscriptionService _subscriptionService;
    private readonly ILogger<GetSubscriptionStatusQueryHandler> _logger;

    public GetSubscriptionStatusQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ISubscriptionService subscriptionService,
        ILogger<GetSubscriptionStatusQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _subscriptionService = subscriptionService;
        _logger = logger;
    }

    public async Task<AppResponse<SubscriptionStatusDto>> Handle(GetSubscriptionStatusQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var hasActiveSubscription = await _subscriptionService.HasActiveSubscription(request.UserId);
            
            var currentSubscriptions = await _unitOfWork.Repository<UserSubscription>()
                .ListAsync(
                    filter: s => s.UserId == request.UserId && s.Status == UserSubscriptionStatus.Active,
                    includeProperties: q => q.Include(s => s.Plan));
            
            var currentSubscription = currentSubscriptions.FirstOrDefault();

            SubscriptionResponseDto? subscriptionDto = null;
            Dictionary<string, object>? planLimits = null;
            Dictionary<string, int>? currentUsage = null;

            if (currentSubscription != null)
            {
                subscriptionDto = _mapper.Map<SubscriptionResponseDto>(currentSubscription);
                var userPlan = await _subscriptionService.GetUserPlan(request.UserId);
                planLimits = await _subscriptionService.GetPlanLimits(userPlan);

                // Get usage for common features
                var commonFeatures = new[] { "recipe_generation", "meal_planning", "smart_recommendations" };
                currentUsage = new Dictionary<string, int>();
                foreach (var feature in commonFeatures)
                {
                    currentUsage[feature] = await _subscriptionService.GetUsageCount(request.UserId, feature, DateTime.UtcNow.Date);
                }
            }

            var statusDto = new SubscriptionStatusDto
            {
                HasActiveSubscription = hasActiveSubscription,
                CurrentSubscription = subscriptionDto,
                PlanLimits = planLimits,
                CurrentUsage = currentUsage
            };

            return new AppResponse<SubscriptionStatusDto>()
                .SetSuccessResponse(statusDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription status for user {UserId}", request.UserId);
            return new AppResponse<SubscriptionStatusDto>()
                .SetErrorResponse("GetSubscriptionStatus", "An error occurred while retrieving subscription status");
        }
    }
} 