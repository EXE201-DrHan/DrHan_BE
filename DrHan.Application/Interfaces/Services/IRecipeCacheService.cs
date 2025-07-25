using DrHan.Application.DTOs.Gemini;
using DrHan.Application.DTOs.Recipes;
using DrHan.Application.Commons;

namespace DrHan.Application.Interfaces.Services;

/// <summary>
/// Enhanced caching service for recipe search optimization
/// </summary>
public interface IRecipeSearchCacheService
{
    /// <summary>
    /// Cache search results with intelligent key generation
    /// </summary>
    Task CacheSearchResultsAsync(string searchKey, IPaginatedList<RecipeDto> results, TimeSpan? expiry = null);

    /// <summary>
    /// Get cached search results
    /// </summary>
    Task<IPaginatedList<RecipeDto>?> GetCachedSearchResultsAsync(string searchKey);

    /// <summary>
    /// Cache AI-generated recipes by search context
    /// </summary>
    Task CacheAIRecipesAsync(string searchContext, List<GeminiRecipeResponseDto> recipes, TimeSpan? expiry = null);

    /// <summary>
    /// Get cached AI recipes for search context
    /// </summary>
    Task<List<GeminiRecipeResponseDto>?> GetCachedAIRecipesAsync(string searchContext);

    /// <summary>
    /// Generate optimized cache key from search parameters
    /// </summary>
    string GenerateSearchCacheKey(RecipeSearchDto searchDto);

    /// <summary>
    /// Invalidate cache for specific search patterns
    /// </summary>
    Task InvalidateSearchCacheAsync(string pattern);

    /// <summary>
    /// Preload popular search results
    /// </summary>
    Task PreloadPopularSearchesAsync(List<string> popularSearchTerms);
} 