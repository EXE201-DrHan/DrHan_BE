using MediatR;
using AutoMapper;
using DrHan.Application.Commons;
using DrHan.Application.DTOs.Subscription;
using DrHan.Application.Interfaces.Repository;
using DrHan.Domain.Entities.Users;
using DrHan.Domain.Constants.Status;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace DrHan.Application.Services.SubscriptionServices.Commands.RenewSubscription;

public class RenewSubscriptionCommandHandler : IRequestHandler<RenewSubscriptionCommand, AppResponse<SubscriptionResponseDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<RenewSubscriptionCommandHandler> _logger;

    public RenewSubscriptionCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<RenewSubscriptionCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<AppResponse<SubscriptionResponseDto>> Handle(RenewSubscriptionCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var subscriptions = await _unitOfWork.Repository<UserSubscription>()
                .ListAsync(
                    filter: s => s.UserId == request.UserId && 
                        (s.Status == UserSubscriptionStatus.Active || s.Status == UserSubscriptionStatus.Expired),
                    includeProperties: q => q.Include(s => s.Plan));
            
            var subscription = subscriptions.FirstOrDefault();

            if (subscription == null)
            {
                return new AppResponse<SubscriptionResponseDto>()
                    .SetErrorResponse("RenewSubscription", "No subscription found for renewal");
            }

            if (subscription.Plan == null)
            {
                return new AppResponse<SubscriptionResponseDto>()
                    .SetErrorResponse("RenewSubscription", "Subscription plan not found");
            }

            subscription.Status = UserSubscriptionStatus.Active;
            subscription.StartDate = DateTime.Now;
            
            subscription.EndDate = subscription.Plan.BillingCycle?.ToLower() switch
            {
                "monthly" => DateTime.Now.AddMonths(1),
                "yearly" => DateTime.Now.AddYears(1),
                "quarterly" => DateTime.Now.AddMonths(3),
                "weekly" => DateTime.Now.AddDays(7),
                _ => DateTime.Now.AddMonths(1)
            };

            _unitOfWork.Repository<UserSubscription>().Update(subscription);
            await _unitOfWork.CompleteAsync();

            var responseDto = _mapper.Map<SubscriptionResponseDto>(subscription);

            _logger.LogInformation("Subscription {SubscriptionId} renewed for user {UserId} until {EndDate}", 
                subscription.Id, request.UserId, subscription.EndDate);

            return new AppResponse<SubscriptionResponseDto>()
                .SetSuccessResponse(responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error renewing subscription for user {UserId}", request.UserId);
            return new AppResponse<SubscriptionResponseDto>()
                .SetErrorResponse("RenewSubscription", "An error occurred while renewing the subscription");
        }
    }
} 