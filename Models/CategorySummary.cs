namespace MenuMint.Models;

// A category plus how many items it holds (result of a JOIN, not a table)
public class CategorySummary
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public int ItemCount { get; set; }
}

public enum DeleteCategoryResult
{
    Deleted,
    NotFound,
    HasItems
}