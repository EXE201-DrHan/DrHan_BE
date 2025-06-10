using MediatR;
using FluentValidation;
using DrHan.Application.DTOs.Allergens;
using DrHan.Application.Commons;

namespace DrHan.Application.Services.AllergenServices.Queries.GetAllergensByCategory;

public class GetAllergensByCategoryQuery : IRequest<AppResponse<IEnumerable<AllergenDto>>>
{
    public string Category { get; set; } = string.Empty;
}

public class GetAllergensByCategoryQueryValidator : AbstractValidator<GetAllergensByCategoryQuery>
{
    public GetAllergensByCategoryQueryValidator()
    {
        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Category is required")
            .MaximumLength(100).WithMessage("Category must not exceed 100 characters");
    }
} 