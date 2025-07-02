using MediatR;
using DrHan.Application.Commons;
using DrHan.Application.DTOs.Subscription;

namespace DrHan.Application.Services.SubscriptionServices.Commands.UpgradeSubscription;

public class UpgradeSubscriptionCommand : IRequest<AppResponse<SubscriptionResponseDto>>
{
    public int UserId { get; set; }
    public int NewPlanId { get; set; }
} 