namespace DrHan.Application.DTOs.Recipes;

public class RecipeDto
{
    public int Id { get; set; }
    public Guid BusinessId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CuisineType { get; set; } = string.Empty;
    public string MealType { get; set; } = string.Empty;
    public int? PrepTimeMinutes { get; set; }
    public int? CookTimeMinutes { get; set; }
    public int? Servings { get; set; }
    public string DifficultyLevel { get; set; } = string.Empty;
    public decimal? RatingAverage { get; set; }
    public int? RatingCount { get; set; }
    public int? LikesCount { get; set; }
    public int? SavesCount { get; set; }
    public bool? IsCustom { get; set; }
    public bool? IsPublic { get; set; }
    public DateTime CreateAt { get; set; }
    public DateTime UpdateAt { get; set; }
    public string? ThumbnailImageUrl { get; set; }
} 