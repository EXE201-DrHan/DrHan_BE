using DrHan.Application.DTOs.Recipes;
using DrHan.Domain.Entities.Recipes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace DrHan.Application.StaticQuery;

public static class RecipeSearchQuery
{
    public static Expression<Func<Recipe, bool>>? BuildFilter(RecipeSearchDto searchDto)
    {
        return recipe =>
            // Start with simple indexed fields first (most selective)
            (string.IsNullOrEmpty(searchDto.CuisineType) ||
            recipe.CuisineType == searchDto.CuisineType) &&

            (string.IsNullOrEmpty(searchDto.MealType) ||
            recipe.MealType == searchDto.MealType) &&

            // Time-based filters (indexed fields)
            (!searchDto.MaxPrepTime.HasValue ||
            recipe.PrepTimeMinutes <= searchDto.MaxPrepTime) &&

            // Simple string searches (avoid multiple Contains in OR)
            (string.IsNullOrEmpty(searchDto.SearchTerm) ||
            recipe.Name.Contains(searchDto.SearchTerm) ||
            recipe.Description.Contains(searchDto.SearchTerm)) &&

            // Simplified ingredient search - avoid complex Any operations
            (string.IsNullOrEmpty(searchDto.SearchTerm) ||
            recipe.RecipeIngredients.Any(ri => ri.IngredientName.Contains(searchDto.SearchTerm))) &&

            // Simplified allergen filters
            (searchDto.ExcludeAllergens == null || !searchDto.ExcludeAllergens.Any() ||
            !recipe.RecipeAllergens.Any(ra => searchDto.ExcludeAllergens.Contains(ra.AllergenType))) &&

            // Simplified allergen-free claims
            (searchDto.RequireAllergenFree == null || !searchDto.RequireAllergenFree.Any() ||
            searchDto.RequireAllergenFree.All(claim =>
                recipe.RecipeAllergenFreeClaims.Any(rc => rc.Claim == claim))) &&

            // Include ingredient filters - simplified
            (searchDto.IncludeIngredients == null || !searchDto.IncludeIngredients.Any() ||
            searchDto.IncludeIngredients.All(ingredient =>
                recipe.RecipeIngredients.Any(ri => ri.IngredientName.Contains(ingredient)))) &&

            // Exclude ingredient filters
            (searchDto.ExcludeIngredients == null || !searchDto.ExcludeIngredients.Any() ||
            !recipe.RecipeIngredients.Any(ri => 
                searchDto.ExcludeIngredients.Any(ingredient => 
                    ri.IngredientName.Contains(ingredient)))) &&

            // Category filter - simplified
            (string.IsNullOrEmpty(searchDto.IngredientCategory) ||
            recipe.RecipeIngredients.Any(ri => 
                ri.IngredientId.HasValue && ri.Ingredient != null && 
                ri.Ingredient.Category != null &&
                ri.Ingredient.Category.Contains(searchDto.IngredientCategory)));
    }

    public static Func<IQueryable<Recipe>, IOrderedQueryable<Recipe>>? BuildOrderBy(RecipeSearchDto searchDto)
    {
        return searchDto.SortBy?.ToLower() switch
        {
            "rating" => searchDto.IsDescending 
                ? query => query.OrderByDescending(r => r.RatingAverage)
                : query => query.OrderBy(r => r.RatingAverage),
            "preptime" => searchDto.IsDescending
                ? query => query.OrderByDescending(r => r.PrepTimeMinutes)
                : query => query.OrderBy(r => r.PrepTimeMinutes),
            "likes" => searchDto.IsDescending
                ? query => query.OrderByDescending(r => r.LikesCount)
                : query => query.OrderBy(r => r.LikesCount),
            _ => searchDto.IsDescending
                ? query => query.OrderByDescending(r => r.Name)
                : query => query.OrderBy(r => r.Name)
        };
    }

    public static Func<IQueryable<Recipe>, IIncludableQueryable<Recipe, object>>? BuildIncludes()
    {
        return query => query
            .Include(r => r.RecipeIngredients)
            .Include(r => r.RecipeInstructions)
            .Include(r => r.RecipeNutritions)
            .Include(r => r.RecipeAllergens)
            .Include(r => r.RecipeAllergenFreeClaims)
            .Include(r => r.RecipeImages);
    }

    /// <summary>
    /// Optimized includes for search results - only loads essential data to avoid performance issues
    /// </summary>
    public static Func<IQueryable<Recipe>, IIncludableQueryable<Recipe, object>>? BuildSearchIncludes()
    {
        // For search results, we only include what's absolutely necessary for filtering and basic display
        // Avoid expensive joins like RecipeInstructions, RecipeNutritions, etc. for search listing
        return query => query
            .Include(r => r.RecipeIngredients.Take(5)) // Limit ingredients for search performance
            .Include(r => r.RecipeAllergens)           // Needed for allergen filtering
            .Include(r => r.RecipeAllergenFreeClaims); // Needed for dietary restriction filtering
        // Removed: .ThenInclude(ri => ri.Ingredient) - This causes expensive joins
        // Load ingredient details separately when needed
    }

    /// <summary>
    /// Minimal includes for basic search - fastest performance
    /// </summary>
    public static Func<IQueryable<Recipe>, IIncludableQueryable<Recipe, object>>? BuildMinimalSearchIncludes()
    {
        // For very fast search when complex filtering isn't needed
        return query => query
            .Include(r => r.RecipeAllergens)
            .Include(r => r.RecipeAllergenFreeClaims);
    }
} 