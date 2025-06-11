using System.ComponentModel.DataAnnotations;

namespace DrHan.Application.DTOs.Recipes;

public class RecipeSearchDto
{
    public string? SearchTerm { get; set; }
    public string? CuisineType { get; set; }
    public string? MealType { get; set; }
    public string? DifficultyLevel { get; set; }
    public int? MaxPrepTime { get; set; }
    public int? MaxCookTime { get; set; }
    public int? MinServings { get; set; }
    public int? MaxServings { get; set; }
    public List<string>? ExcludeAllergens { get; set; }
    public List<string>? RequireAllergenFree { get; set; }
    public bool? IsCustom { get; set; }
    public bool? IsPublic { get; set; }
    public decimal? MinRating { get; set; }
    public string? SortBy { get; set; } = "Name"; // Name, Rating, PrepTime, CookTime, Likes
    public bool IsDescending { get; set; } = false;
    
    [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0")]
    public int Page { get; set; } = 1;
    
    [Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100")]
    public int PageSize { get; set; } = 20;
} 