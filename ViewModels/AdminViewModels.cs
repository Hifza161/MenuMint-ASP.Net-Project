namespace MenuMint.ViewModels;

public class AdminRowVm
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string OwnerEmail { get; set; } = string.Empty;
    public int Items { get; set; }
    public int Categories { get; set; }
    public DateTime Joined { get; set; }
    public bool IsActive { get; set; }
    public bool IsMain { get; set; }
}

public class MonthBarVm
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class AdminDashboardViewModel
{
    public int Total { get; set; }
    public int Active { get; set; }
    public int Inactive { get; set; }
    public int TotalItems { get; set; }
    public List<MonthBarVm> Months { get; set; } = new();
    public List<AdminRowVm> Recent { get; set; } = new();
    public List<AdminRowVm> Largest { get; set; } = new();
    public List<AdminRowVm> Deactivated { get; set; } = new();
    public int ActivePercent => Total == 0 ? 0 : (int)Math.Round(Active * 100.0 / Total);
}