namespace DrHan.Application.DTOs.MealPlans;

public class ShoppingItemDto
{
    public int Id { get; set; }
    public string ItemName { get; set; }
    public decimal? Quantity { get; set; }
    public string Unit { get; set; }
    public string Category { get; set; }
    public bool? IsPurchased { get; set; }
    public decimal? EstimatedCost { get; set; }
} 