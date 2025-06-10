using MediatR;
using FluentValidation;
using DrHan.Application.DTOs.Allergens;
using DrHan.Application.Commons;

namespace DrHan.Application.Services.AllergenServices.Queries.SearchAllergens;

public class SearchAllergensQuery : IRequest<AppResponse<IEnumerable<AllergenDto>>>
{
    public string SearchTerm { get; set; } = string.Empty;
}

public class SearchAllergensQueryValidator : AbstractValidator<SearchAllergensQuery>
{
    public SearchAllergensQueryValidator()
    {
        RuleFor(x => x.SearchTerm)
            .NotEmpty().WithMessage("Search term is required")
            .MinimumLength(2).WithMessage("Search term must be at least 2 characters")
            .MaximumLength(100).WithMessage("Search term cannot exceed 100 characters");
    }
} 