using MediatR;
using AutoMapper;
using DrHan.Application.Commons;
using DrHan.Application.DTOs.Subscription;
using DrHan.Application.Interfaces.Repository;
using DrHan.Domain.Entities.Users;
using DrHan.Domain.Constants.Status;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace DrHan.Application.Services.SubscriptionServices.Commands.UpgradeSubscription;

public class UpgradeSubscriptionCommandHandler : IRequestHandler<UpgradeSubscriptionCommand, AppResponse<SubscriptionResponseDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<UpgradeSubscriptionCommandHandler> _logger;

    public UpgradeSubscriptionCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<UpgradeSubscriptionCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<AppResponse<SubscriptionResponseDto>> Handle(UpgradeSubscriptionCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var currentSubscriptions = await _unitOfWork.Repository<UserSubscription>()
                .ListAsync(
                    filter: s => s.UserId == request.UserId && s.Status == UserSubscriptionStatus.Active,
                    includeProperties: q => q.Include(s => s.Plan));
            
            var currentSubscription = currentSubscriptions.FirstOrDefault();

            if (currentSubscription == null)
            {
                return new AppResponse<SubscriptionResponseDto>()
                    .SetErrorResponse("UpgradeSubscription", "No active subscription found for user");
            }

            var newPlan = await _unitOfWork.Repository<SubscriptionPlan>()
                .FindAsync(p => p.Id == request.NewPlanId && p.IsActive);

            if (newPlan == null)
            {
                return new AppResponse<SubscriptionResponseDto>()
                    .SetErrorResponse("UpgradeSubscription", "New subscription plan not found or inactive");
            }

            if (currentSubscription.Plan != null && newPlan.Price <= currentSubscription.Plan.Price)
            {
                return new AppResponse<SubscriptionResponseDto>()
                    .SetErrorResponse("UpgradeSubscription", "New plan must be higher tier than current plan");
            }

            currentSubscription.PlanId = request.NewPlanId;
            currentSubscription.Plan = newPlan;

            _unitOfWork.Repository<UserSubscription>().Update(currentSubscription);
            await _unitOfWork.CompleteAsync();

            var responseDto = _mapper.Map<SubscriptionResponseDto>(currentSubscription);

            _logger.LogInformation("Subscription upgraded for user {UserId} to plan {NewPlanId}", 
                request.UserId, request.NewPlanId);

            return new AppResponse<SubscriptionResponseDto>()
                .SetSuccessResponse(responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upgrading subscription for user {UserId}", request.UserId);
            return new AppResponse<SubscriptionResponseDto>()
                .SetErrorResponse("UpgradeSubscription", "An error occurred while upgrading the subscription");
        }
    }
} 