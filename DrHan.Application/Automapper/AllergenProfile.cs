using AutoMapper;
using DrHan.Application.DTOs.Allergens;
using DrHan.Domain.Entities.Allergens;
using DrHan.Application.Services.AllergenServices.Commands.CreateAllergen;
using DrHan.Application.Services.AllergenServices.Commands.UpdateAllergen;

namespace DrHan.Application.Automapper;

public class AllergenProfile : Profile
{
    public AllergenProfile()
    {
        CreateMap<Allergen, AllergenDto>();
        CreateMap<CreateAllergenDto, Allergen>();
        CreateMap<UpdateAllergenDto, Allergen>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
        
        // CQRS Command mappings
        CreateMap<CreateAllergenCommand, Allergen>();
        CreateMap<UpdateAllergenCommand, Allergen>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
    }
} 