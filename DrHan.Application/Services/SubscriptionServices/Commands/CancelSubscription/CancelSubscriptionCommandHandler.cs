using MediatR;
using DrHan.Application.Commons;
using DrHan.Application.Interfaces.Repository;
using DrHan.Domain.Entities.Users;
using DrHan.Domain.Constants.Status;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace DrHan.Application.Services.SubscriptionServices.Commands.CancelSubscription;

public class CancelSubscriptionCommandHandler : IRequestHandler<CancelSubscriptionCommand, AppResponse<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CancelSubscriptionCommandHandler> _logger;

    public CancelSubscriptionCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<CancelSubscriptionCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<AppResponse<bool>> Handle(CancelSubscriptionCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var subscriptions = await _unitOfWork.Repository<UserSubscription>()
                .ListAsync(filter: s => s.UserId == request.UserId && s.Status == UserSubscriptionStatus.Active);
            
            var subscription = subscriptions.FirstOrDefault();

            if (subscription == null)
            {
                return new AppResponse<bool>()
                    .SetErrorResponse("CancelSubscription", "No active subscription found for user");
            }

            // Cancel the subscription
            subscription.Status = UserSubscriptionStatus.Cancelled;
            subscription.EndDate = DateTime.UtcNow; // End immediately

            _unitOfWork.Repository<UserSubscription>().Update(subscription);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Subscription {SubscriptionId} cancelled for user {UserId}. Reason: {Reason}", 
                subscription.Id, request.UserId, request.CancellationReason ?? "Not specified");

            return new AppResponse<bool>()
                .SetSuccessResponse(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling subscription for user {UserId}", request.UserId);
            return new AppResponse<bool>()
                .SetErrorResponse("CancelSubscription", "An error occurred while cancelling the subscription");
        }
    }
} 