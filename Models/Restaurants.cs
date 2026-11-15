namespace MenuMint.Models;

public class Restaurant
{
    public int RestaurantId { get; set; }
    public string OwnerUserId { get; set; } = string.Empty;   // Identity user id (string)
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? LogoPath { get; set; }
    public string? CoverPath { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? QrGeneratedAt { get; set; }
}