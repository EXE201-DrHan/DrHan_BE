using MediatR;
using DrHan.Application.Commons;
using DrHan.Application.DTOs.Subscription;

namespace DrHan.Application.Services.SubscriptionServices.Queries.GetSubscriptionStatus;

public class GetSubscriptionStatusQuery : IRequest<AppResponse<SubscriptionStatusDto>>
{
    public int UserId { get; set; }
} 