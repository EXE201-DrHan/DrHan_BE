using MediatR;
using AutoMapper;
using DrHan.Application.Commons;
using DrHan.Application.DTOs.Subscription;
using DrHan.Application.Interfaces.Repository;
using DrHan.Domain.Entities.Users;
using DrHan.Domain.Constants.Status;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace DrHan.Application.Services.SubscriptionServices.Commands.CreateSubscription;

public class CreateSubscriptionCommandHandler : IRequestHandler<CreateSubscriptionCommand, AppResponse<SubscriptionResponseDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateSubscriptionCommandHandler> _logger;

    public CreateSubscriptionCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<CreateSubscriptionCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<AppResponse<SubscriptionResponseDto>> Handle(CreateSubscriptionCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Check if subscription plan exists
            var plan = await _unitOfWork.Repository<SubscriptionPlan>()
                .FindAsync(p => p.Id == request.PlanId && p.IsActive);
            
            if (plan == null)
            {
                return new AppResponse<SubscriptionResponseDto>()
                    .SetErrorResponse("CreateSubscription", "Subscription plan not found or inactive");
            }

            // Check if user already has an active subscription
            var existingSubscriptions = await _unitOfWork.Repository<UserSubscription>()
                .ListAsync(
                    filter: s => s.UserId == request.UserId && s.Status == UserSubscriptionStatus.Active,
                    includeProperties: q => q.Include(s => s.Plan));
            
            var existingSubscription = existingSubscriptions.FirstOrDefault();

            if (existingSubscription != null)
            {
                return new AppResponse<SubscriptionResponseDto>()
                    .SetErrorResponse("CreateSubscription", "User already has an active subscription");
            }

            // Create new subscription with Pending status (will be activated after payment)
            var newSubscription = new UserSubscription
            {
                UserId = request.UserId,
                PlanId = request.PlanId,
                Status = UserSubscriptionStatus.Pending,
                StartDate = DateTime.UtcNow,
                EndDate = null, // Will be set after payment
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<UserSubscription>().AddAsync(newSubscription);
            await _unitOfWork.CompleteAsync();

            // Load the subscription with plan for mapping
            var subscriptionsWithPlan = await _unitOfWork.Repository<UserSubscription>()
                .ListAsync(
                    filter: s => s.Id == newSubscription.Id,
                    includeProperties: q => q.Include(s => s.Plan));
            
            var subscriptionWithPlan = subscriptionsWithPlan.FirstOrDefault();

            var responseDto = _mapper.Map<SubscriptionResponseDto>(subscriptionWithPlan);

            _logger.LogInformation("Subscription created for user {UserId} with plan {PlanId}", 
                request.UserId, request.PlanId);

            return new AppResponse<SubscriptionResponseDto>()
                .SetSuccessResponse(responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating subscription for user {UserId}", request.UserId);
            return new AppResponse<SubscriptionResponseDto>()
                .SetErrorResponse("CreateSubscription", "An error occurred while creating the subscription");
        }
    }
} 