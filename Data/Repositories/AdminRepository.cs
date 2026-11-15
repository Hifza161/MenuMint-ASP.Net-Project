using Dapper;
using MenuMint.Interfaces;
using MenuMint.Interfaces;
using MenuMint.Models;

namespace MenuMint.Data.Repositories;

public class AdminRepository : IAdminRepository
{
    private readonly IDbConnectionFactory _factory;

    public AdminRepository(IDbConnectionFactory factory) => _factory = factory;

    // Restaurant columns first, then the owner columns (the split point is "OwnerName")
    private const string RowSql = @"
        SELECT r.RestaurantId, r.OwnerUserId, r.Name, r.Slug, r.Description, r.Address, r.Phone,
               r.LogoPath, r.CoverPath, r.IsActive, r.CreatedAt, r.QrGeneratedAt,
               u.FullName AS OwnerName, u.Email AS OwnerEmail,
               (SELECT COUNT(*) FROM dbo.MenuItems  i WHERE i.RestaurantId = r.RestaurantId) AS ItemCount,
               (SELECT COUNT(*) FROM dbo.Categories c WHERE c.RestaurantId = r.RestaurantId) AS CategoryCount
        FROM dbo.Restaurants r
        JOIN dbo.AspNetUsers u ON u.Id = r.OwnerUserId";

    // The 'tail' is always a constant written in this class (never user input)
    private async Task<List<AdminRestaurantRow>> QueryRowsAsync(string tail, object? param = null)
    {
        using var db = _factory.CreateConnection();

        // Multi-mapping: each row is split into a Restaurant and an OwnerSummary, then combined
        var rows = await db.QueryAsync<Restaurant, OwnerSummary, AdminRestaurantRow>(
            RowSql + " " + tail,
            (restaurant, owner) => new AdminRestaurantRow
            {
                Restaurant = restaurant,
                OwnerName = owner.OwnerName,
                OwnerEmail = owner.OwnerEmail,
                ItemCount = owner.ItemCount,
                CategoryCount = owner.CategoryCount
            },
            param,
            splitOn: "OwnerName");

        return rows.ToList();
    }

    public Task<List<AdminRestaurantRow>> GetRestaurantsAsync() =>
        QueryRowsAsync("ORDER BY r.CreatedAt DESC");

    public Task<List<AdminRestaurantRow>> GetRecentAsync(int take) =>
        QueryRowsAsync("ORDER BY r.CreatedAt DESC OFFSET 0 ROWS FETCH NEXT @take ROWS ONLY", new { take });

    public Task<List<AdminRestaurantRow>> GetLargestAsync(int take) =>
        QueryRowsAsync("ORDER BY ItemCount DESC, r.Name OFFSET 0 ROWS FETCH NEXT @take ROWS ONLY", new { take });

    public Task<List<AdminRestaurantRow>> GetInactiveAsync() =>
        QueryRowsAsync("WHERE r.IsActive = 0 ORDER BY r.CreatedAt DESC");

    public async Task<PlatformStats> GetStatsAsync()
    {
        const string sql = @"
            SELECT (SELECT COUNT(*) FROM dbo.Restaurants)                  AS Total,
                   (SELECT COUNT(*) FROM dbo.Restaurants WHERE IsActive = 1) AS Active,
                   (SELECT COUNT(*) FROM dbo.Restaurants WHERE IsActive = 0) AS Inactive,
                   (SELECT COUNT(*) FROM dbo.MenuItems)                    AS TotalItems;";

        using var db = _factory.CreateConnection();
        return await db.QuerySingleAsync<PlatformStats>(sql);
    }

    public async Task<List<MonthlySignup>> GetMonthlySignupsAsync(int months)
    {
        var now = DateTime.UtcNow;
        var firstMonth = new DateTime(now.Year, now.Month, 1).AddMonths(-(months - 1));

        const string sql = @"
            SELECT YEAR(CreatedAt) AS [Year], MONTH(CreatedAt) AS [Month], COUNT(*) AS [Count]
            FROM dbo.Restaurants
            WHERE CreatedAt >= @since
            GROUP BY YEAR(CreatedAt), MONTH(CreatedAt);";

        using var db = _factory.CreateConnection();
        var found = (await db.QueryAsync<MonthlySignup>(sql, new { since = firstMonth }))
            .ToDictionary(r => (r.Year, r.Month), r => r.Count);

        // The database only returns months that have signups, so fill in the empty months here
        return Enumerable.Range(0, months)
            .Select(i => firstMonth.AddMonths(i))
            .Select(d => new MonthlySignup
            {
                Year = d.Year,
                Month = d.Month,
                Count = found.TryGetValue((d.Year, d.Month), out var count) ? count : 0
            })
            .ToList();
    }
}