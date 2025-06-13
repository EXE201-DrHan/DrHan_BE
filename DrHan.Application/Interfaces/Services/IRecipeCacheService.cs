using DrHan.Application.DTOs.Gemini;

namespace DrHan.Application.Interfaces.Services;

public interface IRecipeCacheService
{
    /// <summary>
    /// Pre-populate the database with popular recipes
    /// </summary>
    /// <param name="request">Optional request parameters for recipe generation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of recipes added</returns>
    Task<int> PrePopulatePopularRecipesAsync(GeminiRecipeRequestDto? request = null, CancellationToken cancellationToken = default);
} 