using AutoMapper;
using DrHan.Application.DTOs.Users;
using DrHan.Domain.Entities.Users;
using DrHan.Application.Services.UserAllergyServices.Commands.AddUserAllergy;

namespace DrHan.Application.Automapper;

public class UserAllergyProfile : Profile
{
    public UserAllergyProfile()
    {
        CreateMap<UserAllergy, UserAllergyDto>()
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId ?? 0))
            .ForMember(dest => dest.AllergenId, opt => opt.MapFrom(src => src.AllergenId ?? 0));
            
        CreateMap<CreateUserAllergyDto, UserAllergy>();
        
        CreateMap<UpdateUserAllergyDto, UserAllergy>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
        
        // CQRS Command mappings
        CreateMap<AddUserAllergyCommand, UserAllergy>();
    }
} 