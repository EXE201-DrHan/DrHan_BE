using MediatR;
using DrHan.Application.Commons;
using DrHan.Application.DTOs.Subscription;

namespace DrHan.Application.Services.SubscriptionServices.Commands.RenewSubscription;

public class RenewSubscriptionCommand : IRequest<AppResponse<SubscriptionResponseDto>>
{
    public int UserId { get; set; }
} 