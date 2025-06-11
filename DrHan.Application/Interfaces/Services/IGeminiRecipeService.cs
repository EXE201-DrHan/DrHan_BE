using DrHan.Application.DTOs.Gemini;

namespace DrHan.Application.Interfaces.Services;

public interface IGeminiRecipeService
{
    Task<List<GeminiRecipeResponseDto>> SearchRecipesAsync(GeminiRecipeRequestDto request);
} 