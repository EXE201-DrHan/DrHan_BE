using DrHan.Application.DTOs.Recipes;
using FluentValidation;

namespace DrHan.Application.Services.RecipeServices.Queries.SearchRecipes;

public class SearchRecipesQueryValidator : AbstractValidator<SearchRecipesQuery>
{
    public SearchRecipesQueryValidator()
    {
        RuleFor(x => x.SearchDto)
            .NotNull()
            .WithMessage("Search parameters are required");

        RuleFor(x => x.SearchDto.Page)
            .GreaterThan(0)
            .WithMessage("Page must be greater than 0");

        RuleFor(x => x.SearchDto.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Page size must be between 1 and 100");

        RuleFor(x => x.SearchDto.MaxPrepTime)
            .GreaterThan(0)
            .When(x => x.SearchDto.MaxPrepTime.HasValue)
            .WithMessage("Maximum prep time must be greater than 0");

        RuleFor(x => x.SearchDto.MaxCookTime)
            .GreaterThan(0)
            .When(x => x.SearchDto.MaxCookTime.HasValue)
            .WithMessage("Maximum cook time must be greater than 0");

        RuleFor(x => x.SearchDto.MinServings)
            .GreaterThan(0)
            .When(x => x.SearchDto.MinServings.HasValue)
            .WithMessage("Minimum servings must be greater than 0");

        RuleFor(x => x.SearchDto.MaxServings)
            .GreaterThan(0)
            .When(x => x.SearchDto.MaxServings.HasValue)
            .WithMessage("Maximum servings must be greater than 0");

        RuleFor(x => x.SearchDto)
            .Must(x => !x.MinServings.HasValue || !x.MaxServings.HasValue || x.MinServings <= x.MaxServings)
            .WithMessage("Minimum servings cannot be greater than maximum servings");

        RuleFor(x => x.SearchDto.MinRating)
            .InclusiveBetween(0, 5)
            .When(x => x.SearchDto.MinRating.HasValue)
            .WithMessage("Minimum rating must be between 0 and 5");

        RuleFor(x => x.SearchDto.SortBy)
            .Must(BeValidSortField)
            .When(x => !string.IsNullOrEmpty(x.SearchDto.SortBy))
            .WithMessage("Sort field must be one of: Name, Rating, PrepTime, CookTime, Likes");
    }

    private static bool BeValidSortField(string? sortBy)
    {
        if (string.IsNullOrEmpty(sortBy))
            return true;

        var validSortFields = new[] { "Name", "Rating", "PrepTime", "CookTime", "Likes" };
        return validSortFields.Contains(sortBy, StringComparer.OrdinalIgnoreCase);
    }
} 