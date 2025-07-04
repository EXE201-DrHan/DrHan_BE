using AutoMapper;
using DrHan.Application.DTOs.Subscription;
using DrHan.Domain.Entities.Users;

namespace DrHan.Application.Automapper;

public class SubscriptionProfile : Profile
{
    public SubscriptionProfile()
    {
        CreateMap<UserSubscription, SubscriptionResponseDto>()
            .ForMember(dest => dest.PlanName, opt => opt.MapFrom(src => src.Plan != null ? src.Plan.Name : ""))
            .ForMember(dest => dest.PlanPrice, opt => opt.MapFrom(src => src.Plan != null ? src.Plan.Price : 0))
            .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Plan != null ? src.Plan.Currency : "VND"))
            .ForMember(dest => dest.BillingCycle, opt => opt.MapFrom(src => src.Plan != null ? src.Plan.BillingCycle : ""))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.Status == Domain.Constants.Status.UserSubscriptionStatus.Active && 
                (src.EndDate == null || src.EndDate > DateTime.Now)))
            .ForMember(dest => dest.DaysRemaining, opt => opt.MapFrom(src => src.EndDate.HasValue ? 
                (int?)Math.Max(0, (src.EndDate.Value - DateTime.Now).Days) : null));

        CreateMap<Payment, PurchaseHistoryDto>()
            .ForMember(dest => dest.PlanName, opt => opt.MapFrom(src => src.UserSubscription != null && src.UserSubscription.Plan != null ? src.UserSubscription.Plan.Name : ""))
            .ForMember(dest => dest.BillingCycle, opt => opt.MapFrom(src => src.UserSubscription != null && src.UserSubscription.Plan != null ? src.UserSubscription.Plan.BillingCycle : ""));

        CreateMap<SubscriptionUsage, UsageHistoryDto>()
            .ForMember(dest => dest.PlanName, opt => opt.MapFrom(src => src.UserSubscription != null && src.UserSubscription.Plan != null ? src.UserSubscription.Plan.Name : ""));

        CreateMap<UserSubscription, SubscriptionHistoryDto>()
            .ForMember(dest => dest.PlanName, opt => opt.MapFrom(src => src.Plan != null ? src.Plan.Name : ""))
            .ForMember(dest => dest.PlanPrice, opt => opt.MapFrom(src => src.Plan != null ? src.Plan.Price : 0))
            .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Plan != null ? src.Plan.Currency : "VND"))
            .ForMember(dest => dest.BillingCycle, opt => opt.MapFrom(src => src.Plan != null ? src.Plan.BillingCycle : ""))
            .ForMember(dest => dest.DaysActive, opt => opt.MapFrom(src => CalculateDaysActive(src)));

        // Plan Management Mappings
        CreateMap<SubscriptionPlan, SubscriptionPlanDto>()
            .ForMember(dest => dest.Features, opt => opt.MapFrom(src => src.PlanFeatures));

        CreateMap<CreateSubscriptionPlanDto, SubscriptionPlan>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.Now))
            .ForMember(dest => dest.PlanFeatures, opt => opt.Ignore()); // Will be handled separately

        CreateMap<UpdateSubscriptionPlanDto, SubscriptionPlan>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.PlanFeatures, opt => opt.Ignore())
            .ForMember(dest => dest.UserSubscriptions, opt => opt.Ignore());

        CreateMap<PlanFeature, PlanFeatureDto>();

        CreateMap<CreatePlanFeatureDto, PlanFeature>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.Now));

        CreateMap<UpdatePlanFeatureDto, PlanFeature>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.PlanId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Plan, opt => opt.Ignore());

        // Admin User Subscription Mapping
        CreateMap<UserSubscription, AdminUserSubscriptionDto>()
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.ApplicationUser != null ? src.ApplicationUser.UserName : ""))
            .ForMember(dest => dest.UserEmail, opt => opt.MapFrom(src => src.ApplicationUser != null ? src.ApplicationUser.Email : ""))
            .ForMember(dest => dest.PlanName, opt => opt.MapFrom(src => src.Plan != null ? src.Plan.Name : ""))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));
    }

    private static int CalculateDaysActive(UserSubscription subscription)
    {
        var endDate = subscription.EndDate ?? DateTime.Now;
        var startDate = subscription.StartDate;
        return Math.Max(0, (int)(endDate - startDate).TotalDays);
    }
} 