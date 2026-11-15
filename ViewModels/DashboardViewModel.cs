namespace MenuMint.ViewModels;

public class DashboardViewModel
{
    public string OwnerName { get; set; } = string.Empty;
    public string RestaurantName { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;

    public int CategoryCount { get; set; }
    public int ItemCount { get; set; }
    public int AvailableCount { get; set; }
    public int SoldOutCount { get; set; }

    public bool HasProfile { get; set; }
    public bool HasQr { get; set; }

    public List<RecentItemVm> RecentItems { get; set; } = new();

    public int ProgressPercent
    {
        get
        {
            int done = 0;
            if (HasProfile) done++;
            if (CategoryCount > 0) done++;
            if (ItemCount > 0) done++;
            if (HasQr) done++;
            return done * 25;
        }
    }
}

public class RecentItemVm
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Emoji { get; set; } = "🍽️";
    public decimal Price { get; set; }
    public bool IsAvailable { get; set; }
    public string? ImagePath { get; set; }
}