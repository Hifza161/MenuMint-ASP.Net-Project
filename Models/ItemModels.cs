namespace MenuMint.Models;

// A menu item plus the name of its category (result of a JOIN)
public class MenuItemRow : MenuItem
{
    public string CategoryName { get; set; } = string.Empty;
}

public class ItemCounts
{
    public int Total { get; set; }
    public int Available { get; set; }
    public int SoldOut { get; set; }
}

public class ItemToggleResult
{
    public bool IsAvailable { get; set; }
    public string Name { get; set; } = string.Empty;
}

// Returned by a delete so the controller can also remove the image file
public class DeletedItem
{
    public string Name { get; set; } = string.Empty;
    public string? ImagePath { get; set; }
}