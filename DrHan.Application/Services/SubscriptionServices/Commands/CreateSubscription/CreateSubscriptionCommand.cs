using MediatR;
using DrHan.Application.Commons;
using DrHan.Application.DTOs.Subscription;

namespace DrHan.Application.Services.SubscriptionServices.Commands.CreateSubscription;

public class CreateSubscriptionCommand : IRequest<AppResponse<SubscriptionResponseDto>>
{
    public int UserId { get; set; }
    public int PlanId { get; set; }
} 