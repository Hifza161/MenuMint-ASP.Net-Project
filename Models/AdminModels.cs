namespace MenuMint.Models;

// The second half of the admin JOIN (owner details plus counts)
public class OwnerSummary
{
    public string OwnerName { get; set; } = string.Empty;
    public string OwnerEmail { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public int CategoryCount { get; set; }
}

public class AdminRestaurantRow
{
    public Restaurant Restaurant { get; set; } = null!;
    public string OwnerName { get; set; } = string.Empty;
    public string OwnerEmail { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public int CategoryCount { get; set; }
}

public class PlatformStats
{
    public int Total { get; set; }
    public int Active { get; set; }
    public int Inactive { get; set; }
    public int TotalItems { get; set; }
}

public class MonthlySignup
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int Count { get; set; }
}