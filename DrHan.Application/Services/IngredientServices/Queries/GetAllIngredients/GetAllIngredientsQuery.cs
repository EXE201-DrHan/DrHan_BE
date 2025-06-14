using MediatR;
using FluentValidation;
using DrHan.Application.DTOs.Ingredients;
using DrHan.Application.Commons;

namespace DrHan.Application.Services.IngredientServices.Queries.GetAllIngredients;

public class GetAllIngredientsQuery : IRequest<AppResponse<IPaginatedList<IngredientDto>>>
{
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 20;
    public string? Search { get; set; }
    public string? Category { get; set; }
}

public class GetAllIngredientsQueryValidator : AbstractValidator<GetAllIngredientsQuery>
{
    public GetAllIngredientsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("Page must be greater than 0");

        RuleFor(x => x.Size)
            .GreaterThan(0).WithMessage("Size must be greater than 0")
            .LessThanOrEqualTo(100).WithMessage("Size cannot exceed 100");

        RuleFor(x => x.Search)
            .MaximumLength(100).WithMessage("Search term cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.Search));

        RuleFor(x => x.Category)
            .MaximumLength(100).WithMessage("Category cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.Category));
    }
} 