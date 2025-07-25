using DrHan.Application.DTOs.Gemini;

namespace DrHan.Application.Interfaces.Services;

/// <summary>
/// Service for handling asynchronous recipe persistence operations
/// </summary>
public interface IRecipePersistenceService
{
    /// <summary>
    /// Queue recipes for background persistence to database
    /// </summary>
    /// <param name="geminiRecipes">AI-generated recipes to persist</param>
    /// <param name="searchContext">Context information about the search that generated these recipes</param>
    Task QueueRecipesForPersistenceAsync(List<GeminiRecipeResponseDto> geminiRecipes, string searchContext);

    /// <summary>
    /// Process queued recipes and persist them to database
    /// </summary>
    Task ProcessQueuedRecipesAsync(CancellationToken cancellationToken = default);
}