namespace MenuMint.ViewModels;

public class PublicMenuViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? LogoPath { get; set; }
    public List<PublicSectionVm> Sections { get; set; } = new();
    public string? CoverPath { get; set; }
}

public class PublicSectionVm
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<PublicItemVm> Items { get; set; } = new();
}

public class PublicItemVm
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Emoji { get; set; } = "🍽️";
    public string? ImagePath { get; set; }
    public bool IsAvailable { get; set; }
}