using Dapper;
using MenuMint.Interfaces;
using MenuMint.Interfaces;
using MenuMint.Models;

namespace MenuMint.Data.Repositories;

public class MenuItemRepository : IMenuItemRepository
{
    private readonly IDbConnectionFactory _factory;

    public MenuItemRepository(IDbConnectionFactory factory) => _factory = factory;

    // Constant column list (no user input)
    private const string ItemColumns =
        "i.MenuItemId, i.RestaurantId, i.CategoryId, i.Name, i.Description, i.Price, i.Emoji, i.ImagePath, i.IsAvailable";

    public async Task<List<MenuItemRow>> GetByRestaurantAsync(int restaurantId)
    {
        const string sql = @"
            SELECT " + ItemColumns + @", c.Name AS CategoryName
            FROM dbo.MenuItems i
            JOIN dbo.Categories c ON c.CategoryId = i.CategoryId
            WHERE i.RestaurantId = @restaurantId
            ORDER BY c.DisplayOrder, i.Name;";

        using var db = _factory.CreateConnection();
        return (await db.QueryAsync<MenuItemRow>(sql, new { restaurantId })).ToList();
    }

    public async Task<List<MenuItemRow>> GetRecentAsync(int restaurantId, int take)
    {
        const string sql = @"
            SELECT TOP (@take) " + ItemColumns + @", c.Name AS CategoryName
            FROM dbo.MenuItems i
            JOIN dbo.Categories c ON c.CategoryId = i.CategoryId
            WHERE i.RestaurantId = @restaurantId
            ORDER BY i.MenuItemId DESC;";

        using var db = _factory.CreateConnection();
        return (await db.QueryAsync<MenuItemRow>(sql, new { restaurantId, take })).ToList();
    }

    public async Task<MenuItem?> GetAsync(int menuItemId, int restaurantId)
    {
        const string sql = @"
            SELECT " + ItemColumns + @"
            FROM dbo.MenuItems i
            WHERE i.MenuItemId = @menuItemId AND i.RestaurantId = @restaurantId;";

        using var db = _factory.CreateConnection();
        return await db.QuerySingleOrDefaultAsync<MenuItem>(sql, new { menuItemId, restaurantId });
    }

    public async Task<int> CreateAsync(MenuItem item)
    {
        // INSERT ... SELECT ... WHERE EXISTS: no row is inserted unless the category belongs to this restaurant
        const string sql = @"
            INSERT INTO dbo.MenuItems (RestaurantId, CategoryId, Name, Description, Price, Emoji, ImagePath, IsAvailable)
            OUTPUT INSERTED.MenuItemId
            SELECT @RestaurantId, @CategoryId, @Name, @Description, @Price, @Emoji, @ImagePath, @IsAvailable
            WHERE EXISTS (SELECT 1 FROM dbo.Categories
                          WHERE CategoryId = @CategoryId AND RestaurantId = @RestaurantId);";

        using var db = _factory.CreateConnection();
        return await db.ExecuteScalarAsync<int?>(sql, item) ?? 0;
    }

    public async Task<bool> UpdateAsync(MenuItem item)
    {
        const string sql = @"
            UPDATE dbo.MenuItems
            SET CategoryId = @CategoryId, Name = @Name, Description = @Description, Price = @Price,
                Emoji = @Emoji, ImagePath = @ImagePath, IsAvailable = @IsAvailable
            WHERE MenuItemId = @MenuItemId AND RestaurantId = @RestaurantId
              AND EXISTS (SELECT 1 FROM dbo.Categories
                          WHERE CategoryId = @CategoryId AND RestaurantId = @RestaurantId);";

        using var db = _factory.CreateConnection();
        return await db.ExecuteAsync(sql, item) > 0;
    }

    public async Task<ItemToggleResult?> ToggleAvailabilityAsync(int menuItemId, int restaurantId)
    {
        // One statement flips the flag and returns the new value (OUTPUT clause)
        const string sql = @"
            UPDATE dbo.MenuItems
            SET IsAvailable = CASE WHEN IsAvailable = 1 THEN 0 ELSE 1 END
            OUTPUT INSERTED.IsAvailable AS IsAvailable, INSERTED.Name AS Name
            WHERE MenuItemId = @menuItemId AND RestaurantId = @restaurantId;";

        using var db = _factory.CreateConnection();
        return await db.QuerySingleOrDefaultAsync<ItemToggleResult>(sql, new { menuItemId, restaurantId });
    }

    public async Task<DeletedItem?> DeleteAsync(int menuItemId, int restaurantId)
    {
        const string sql = @"
            DELETE FROM dbo.MenuItems
            OUTPUT DELETED.Name AS Name, DELETED.ImagePath AS ImagePath
            WHERE MenuItemId = @menuItemId AND RestaurantId = @restaurantId;";

        using var db = _factory.CreateConnection();
        return await db.QuerySingleOrDefaultAsync<DeletedItem>(sql, new { menuItemId, restaurantId });
    }

    public async Task<ItemCounts> GetCountsAsync(int restaurantId)
    {
        const string sql = @"
            SELECT COUNT(*) AS Total,
                   ISNULL(SUM(CASE WHEN IsAvailable = 1 THEN 1 ELSE 0 END), 0) AS Available,
                   ISNULL(SUM(CASE WHEN IsAvailable = 0 THEN 1 ELSE 0 END), 0) AS SoldOut
            FROM dbo.MenuItems
            WHERE RestaurantId = @restaurantId;";

        using var db = _factory.CreateConnection();
        return await db.QuerySingleAsync<ItemCounts>(sql, new { restaurantId });
    }
}