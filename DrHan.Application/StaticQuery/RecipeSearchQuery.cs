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
            // Basic recipe properties search (most efficient first)
            (string.IsNullOrEmpty(searchDto.SearchTerm) ||
            recipe.Name.Contains(searchDto.SearchTerm) ||
            recipe.Description.Contains(searchDto.SearchTerm)) &&

            // Filter by cuisine type (indexed field)
            (string.IsNullOrEmpty(searchDto.CuisineType) ||
            recipe.CuisineType == searchDto.CuisineType) &&

            // Filter by meal type (indexed field)
            (string.IsNullOrEmpty(searchDto.MealType) ||
            recipe.MealType == searchDto.MealType) &&

            // Time-based filters (indexed fields)
            (!searchDto.MaxPrepTime.HasValue ||
            recipe.PrepTimeMinutes <= searchDto.MaxPrepTime) &&

            // Complex related entity filters (moved to end for performance)
            (string.IsNullOrEmpty(searchDto.SearchTerm) ||
            recipe.RecipeIngredients.Any(ri => ri.IngredientName.Contains(searchDto.SearchTerm))) &&

            // Allergen filters
            (searchDto.ExcludeAllergens == null || !searchDto.ExcludeAllergens.Any() ||
            !recipe.RecipeAllergens.Any(ra => searchDto.ExcludeAllergens.Contains(ra.AllergenType))) &&

            (searchDto.RequireAllergenFree == null || !searchDto.RequireAllergenFree.Any() ||
            searchDto.RequireAllergenFree.All(claim =>
                recipe.RecipeAllergenFreeClaims.Any(rc => rc.Claim == claim))) &&

            // Ingredient filters
            (searchDto.IncludeIngredients == null || !searchDto.IncludeIngredients.Any() ||
            searchDto.IncludeIngredients.All(ingredient =>
                recipe.RecipeIngredients.Any(ri => ri.IngredientName.ToLower().Contains(ingredient.ToLower())))) &&

            (searchDto.ExcludeIngredients == null || !searchDto.ExcludeIngredients.Any() ||
            !recipe.RecipeIngredients.Any(ri => 
                searchDto.ExcludeIngredients.Any(ingredient => 
                    ri.IngredientName.ToLower().Contains(ingredient.ToLower())))) &&

            // Ingredient category filter (requires linked ingredients)
            (string.IsNullOrEmpty(searchDto.IngredientCategory) ||
            recipe.RecipeIngredients.Any(ri => 
                ri.IngredientId.HasValue && ri.Ingredient != null && 
                ri.Ingredient.Category != null &&
                ri.Ingredient.Category.ToLower().Contains(searchDto.IngredientCategory.ToLower())));
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
        // For search results, we need ingredients for filtering and basic display
        // Include linked ingredient entities for category filtering
        return query => query
            .Include(r => r.RecipeIngredients)
                .ThenInclude(ri => ri.Ingredient); // Include for ingredient category filtering
    }
} 