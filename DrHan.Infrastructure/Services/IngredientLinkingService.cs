using DrHan.Application.Interfaces.Repository;
using DrHan.Domain.Entities.Ingredients;
using Microsoft.Extensions.Logging;

namespace DrHan.Infrastructure.Services;

public interface IIngredientLinkingService
{
    Task<Ingredient?> FindOrCreateIngredientAsync(string ingredientName, IUnitOfWork unitOfWork);
}

public class IngredientLinkingService : IIngredientLinkingService
{
    private readonly ILogger<IngredientLinkingService> _logger;

    public IngredientLinkingService(ILogger<IngredientLinkingService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Ingredient?> FindOrCreateIngredientAsync(string ingredientName, IUnitOfWork unitOfWork)
    {
        try
        {
            // Try to find existing ingredient by exact name match
            var ingredients = await unitOfWork.Repository<Ingredient>().ListAsync(
                filter: i => i.Name.ToLower() == ingredientName.ToLower()
            );

            if (ingredients.Any())
            {
                return ingredients.First();
            }

            // Try to find by similar name (using contains)
            var similarIngredients = await unitOfWork.Repository<Ingredient>().ListAsync(
                filter: i => i.Name.ToLower().Contains(ingredientName.ToLower()) || 
                           ingredientName.ToLower().Contains(i.Name.ToLower())
            );

            if (similarIngredients.Any())
            {
                _logger.LogDebug("Found similar ingredient '{ExistingName}' for '{NewName}'", 
                    similarIngredients.First().Name, ingredientName);
                return similarIngredients.First();
            }

            // Auto-create missing ingredient with smart category matching
            var category = await FindOrCreateIngredientCategory(ingredientName, unitOfWork);
            
            var newIngredient = new Ingredient
            {
                BusinessId = Guid.NewGuid(),
                Name = ingredientName,
                Category = category,
                Description = $"Auto-generated ingredient from AI recipe",
                CreateAt = DateTime.UtcNow,
                UpdateAt = DateTime.UtcNow
            };

            await unitOfWork.Repository<Ingredient>().AddAsync(newIngredient);
            _logger.LogInformation("Created new ingredient: '{IngredientName}' in category '{Category}'", 
                ingredientName, category);
            return newIngredient;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error finding/creating ingredient: {IngredientName}", ingredientName);
            return null;
        }
    }

    private async Task<string> FindOrCreateIngredientCategory(string ingredientName, IUnitOfWork unitOfWork)
    {
        try
        {
            // Get all existing categories
            var existingIngredients = await unitOfWork.Repository<Ingredient>().ListAsync();
            var existingCategories = existingIngredients
                .Where(i => !string.IsNullOrEmpty(i.Category))
                .Select(i => i.Category)
                .Distinct()
                .ToList();

            if (!existingCategories.Any())
            {
                return "AI Generated"; // Fallback if no categories exist
            }

            // Smart category matching based on ingredient name
            var lowerName = ingredientName.ToLower();
            
            // Vietnamese/Asian ingredient patterns
            if (ContainsAny(lowerName, new[] { "nước mắm", "tương", "miso", "sake", "mirin", "kimchi" }))
                return FindBestCategoryMatch(existingCategories, new[] { "gia vị", "sauce", "condiment", "seasoning" });
            
            if (ContainsAny(lowerName, new[] { "thịt", "gà", "vịt", "heo", "bò", "chicken", "beef", "pork", "meat" }))
                return FindBestCategoryMatch(existingCategories, new[] { "thịt", "meat", "protein" });
            
            if (ContainsAny(lowerName, new[] { "cá", "tôm", "cua", "mực", "fish", "shrimp", "seafood" }))
                return FindBestCategoryMatch(existingCategories, new[] { "hải sản", "seafood", "fish" });
            
            if (ContainsAny(lowerName, new[] { "rau", "cải", "salad", "vegetable", "lettuce", "spinach" }))
                return FindBestCategoryMatch(existingCategories, new[] { "rau", "vegetable", "greens" });
            
            if (ContainsAny(lowerName, new[] { "trái", "quả", "fruit", "apple", "banana", "orange" }))
                return FindBestCategoryMatch(existingCategories, new[] { "trái cây", "fruit" });
            
            if (ContainsAny(lowerName, new[] { "gạo", "bánh", "bột", "rice", "flour", "bread", "grain" }))
                return FindBestCategoryMatch(existingCategories, new[] { "tinh bột", "grain", "carbohydrate" });
            
            if (ContainsAny(lowerName, new[] { "sữa", "phô mai", "yogurt", "milk", "cheese", "dairy" }))
                return FindBestCategoryMatch(existingCategories, new[] { "sữa", "dairy" });
            
            if (ContainsAny(lowerName, new[] { "dầu", "mỡ", "oil", "butter", "fat" }))
                return FindBestCategoryMatch(existingCategories, new[] { "dầu mỡ", "oil", "fat" });
            
            if (ContainsAny(lowerName, new[] { "gia vị", "tiêu", "muối", "spice", "pepper", "salt", "herb" }))
                return FindBestCategoryMatch(existingCategories, new[] { "gia vị", "spice", "seasoning" });

            // If no pattern matches, try to find a generic category
            var genericCategory = FindBestCategoryMatch(existingCategories, new[] { "other", "khác", "general", "ai generated" });
            if (!string.IsNullOrEmpty(genericCategory))
                return genericCategory;

            // Create new category based on ingredient type
            return CreateNewCategoryName(ingredientName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error determining category for ingredient: {IngredientName}", ingredientName);
            return "AI Generated";
        }
    }

    private bool ContainsAny(string text, string[] keywords)
    {
        return keywords.Any(keyword => text.Contains(keyword));
    }

    private string FindBestCategoryMatch(List<string> existingCategories, string[] preferredCategories)
    {
        foreach (var preferred in preferredCategories)
        {
            var match = existingCategories.FirstOrDefault(cat => 
                cat.ToLower().Contains(preferred.ToLower()) || preferred.ToLower().Contains(cat.ToLower()));
            if (!string.IsNullOrEmpty(match))
                return match;
        }
        return string.Empty;
    }

    private string CreateNewCategoryName(string ingredientName)
    {
        var lowerName = ingredientName.ToLower();
        
        // Create meaningful category names for Vietnamese ingredients
        if (ContainsAny(lowerName, new[] { "lá", "leaf" }))
            return "Lá gia vị"; // Herb leaves
        
        if (ContainsAny(lowerName, new[] { "hạt", "seed", "nut" }))
            return "Hạt & Đậu"; // Seeds & Nuts
        
        if (ContainsAny(lowerName, new[] { "nước", "liquid", "broth" }))
            return "Nước dùng"; // Liquids/Broths
        
        return "AI Generated"; // Default fallback
    }
} 