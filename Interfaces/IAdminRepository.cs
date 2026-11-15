using MenuMint.Models;

namespace MenuMint.Interfaces;

public interface IAdminRepository
{
    Task<List<AdminRestaurantRow>> GetRestaurantsAsync();             // newest first
    Task<List<AdminRestaurantRow>> GetRecentAsync(int take);          // newest first, limited
    Task<List<AdminRestaurantRow>> GetLargestAsync(int take);         // most items first
    Task<List<AdminRestaurantRow>> GetInactiveAsync();
    Task<PlatformStats> GetStatsAsync();

    /// <summary>One entry per month for the last N months, including months with zero signups.</summary>
    Task<List<MonthlySignup>> GetMonthlySignupsAsync(int months);
}