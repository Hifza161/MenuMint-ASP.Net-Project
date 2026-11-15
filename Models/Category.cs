namespace MenuMint.Models;

public class Category
{
    public int CategoryId { get; set; }
    public int RestaurantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}