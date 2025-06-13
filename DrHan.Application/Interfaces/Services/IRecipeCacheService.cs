namespace DrHan.Application.Interfaces.Services;

public interface IRecipeCacheService
{
    /// <summary>
    /// Pre-populate the database with popular recipes
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of recipes added</returns>
    Task<int> PrePopulatePopularRecipesAsync(CancellationToken cancellationToken = default);
} 