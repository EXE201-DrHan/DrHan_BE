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