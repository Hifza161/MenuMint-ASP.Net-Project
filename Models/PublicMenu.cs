namespace MenuMint.Models;

public class PublicMenu
{
    public Restaurant Restaurant { get; set; } = null!;
    public List<PublicMenuSection> Sections { get; set; } = new();
}

public class PublicMenuSection
{
    public Category Category { get; set; } = null!;
    public List<MenuItem> Items { get; set; } = new();
}