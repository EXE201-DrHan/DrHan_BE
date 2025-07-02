using MediatR;
using DrHan.Application.Commons;

namespace DrHan.Application.Services.SubscriptionServices.Commands.CancelSubscription;

public class CancelSubscriptionCommand : IRequest<AppResponse<bool>>
{
    public int UserId { get; set; }
    public string? CancellationReason { get; set; }
} 