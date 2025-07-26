using DrHan.Application.DTOs.Gemini;

namespace DrHan.Infrastructure.Services
{
    public interface IRecipeCacheService
    {
        Task<int> PrePopulatePopularRecipesAsync(GeminiRecipeRequestDto? request = null, CancellationToken cancellationToken = default);
    }
}