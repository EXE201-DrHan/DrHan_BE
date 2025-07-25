using AutoMapper;
using DrHan.Application.DTOs.Notifications;
using DrHan.Domain.Entities.Notifications;
using DrHan.Domain.Entities.Users;

namespace DrHan.Application.Automapper;

public class MealNotificationProfile : Profile
{
    public MealNotificationProfile()
    {
        // UserMealNotificationSettings mappings
        CreateMap<UserMealNotificationSettings, UserMealNotificationSettingsDto>()
            .ForMember(dest => dest.BreakfastTime, opt => opt.MapFrom(src => src.BreakfastTime.HasValue ? src.BreakfastTime.Value.ToString("HH:mm") : null))
            .ForMember(dest => dest.LunchTime, opt => opt.MapFrom(src => src.LunchTime.HasValue ? src.LunchTime.Value.ToString("HH:mm") : null))
            .ForMember(dest => dest.DinnerTime, opt => opt.MapFrom(src => src.DinnerTime.HasValue ? src.DinnerTime.Value.ToString("HH:mm") : null))
            .ForMember(dest => dest.SnackTime, opt => opt.MapFrom(src => src.SnackTime.HasValue ? src.SnackTime.Value.ToString("HH:mm") : null))
            .ForMember(dest => dest.QuietStartTime, opt => opt.MapFrom(src => src.QuietStartTime.HasValue ? src.QuietStartTime.Value.ToString("HH:mm") : null))
            .ForMember(dest => dest.QuietEndTime, opt => opt.MapFrom(src => src.QuietEndTime.HasValue ? src.QuietEndTime.Value.ToString("HH:mm") : null));

        CreateMap<UpdateMealNotificationSettingsDto, UserMealNotificationSettings>()
            .ForMember(dest => dest.BreakfastTime, opt => opt.Ignore()) // Handled manually in service
            .ForMember(dest => dest.LunchTime, opt => opt.Ignore())
            .ForMember(dest => dest.DinnerTime, opt => opt.Ignore())
            .ForMember(dest => dest.SnackTime, opt => opt.Ignore())
            .ForMember(dest => dest.QuietStartTime, opt => opt.Ignore())
            .ForMember(dest => dest.QuietEndTime, opt => opt.Ignore())
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreateAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdateAt, opt => opt.Ignore())
            .ForMember(dest => dest.User, opt => opt.Ignore());

        // MealNotificationLog mappings
        CreateMap<MealNotificationLog, MealNotificationLogDto>()
            .ForMember(dest => dest.MealName, opt => opt.MapFrom(src => 
                // This would need to be populated from the meal entry if needed
                $"Meal Entry {src.MealPlanEntryId}"));
    }
} 