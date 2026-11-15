namespace MenuMint.Models;

public class MenuItem
{
    public int MenuItemId { get; set; }
    public int RestaurantId { get; set; }
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Emoji { get; set; } = "🍽️";
    public string? ImagePath { get; set; }
    public bool IsAvailable { get; set; } = true;
}