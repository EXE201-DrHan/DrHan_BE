using MediatR;
using FluentValidation;
using DrHan.Application.DTOs.Allergens;
using DrHan.Application.Commons;

namespace DrHan.Application.Services.AllergenServices.Queries.GetMajorAllergens;

public class GetMajorAllergensQuery : IRequest<AppResponse<IEnumerable<AllergenDto>>>
{
    public bool? IsFdaMajor { get; set; }
    public bool? IsEuMajor { get; set; }
}

public class GetMajorAllergensQueryValidator : AbstractValidator<GetMajorAllergensQuery>
{
    public GetMajorAllergensQueryValidator()
    {
        // At least one major type should be specified
        RuleFor(x => x)
            .Must(x => x.IsFdaMajor.HasValue || x.IsEuMajor.HasValue)
            .WithMessage("At least one major allergen type (FDA or EU) must be specified");
    }
} 