using Dapper;
using MenuMint.Interfaces;
using MenuMint.Models;

namespace MenuMint.Data.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly IDbConnectionFactory _factory;

    public CategoryRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<List<CategorySummary>> GetSummariesAsync(int restaurantId)
    {
        const string sql = @"
            SELECT c.CategoryId, c.Name, c.DisplayOrder, c.IsActive, COUNT(i.MenuItemId) AS ItemCount
            FROM dbo.Categories c
            LEFT JOIN dbo.MenuItems i ON i.CategoryId = c.CategoryId
            WHERE c.RestaurantId = @restaurantId
            GROUP BY c.CategoryId, c.Name, c.DisplayOrder, c.IsActive
            ORDER BY c.DisplayOrder;";

        using var db = _factory.CreateConnection();
        return (await db.QueryAsync<CategorySummary>(sql, new { restaurantId })).ToList();
    }

    public async Task<List<Category>> GetAllAsync(int restaurantId)
    {
        using var db = _factory.CreateConnection();
        return (await db.QueryAsync<Category>(
            @"SELECT CategoryId, RestaurantId, Name, DisplayOrder, IsActive
              FROM dbo.Categories WHERE RestaurantId = @restaurantId ORDER BY DisplayOrder",
            new { restaurantId })).ToList();
    }

    public async Task<Category?> GetAsync(int categoryId, int restaurantId)
    {
        using var db = _factory.CreateConnection();
        return await db.QuerySingleOrDefaultAsync<Category>(
            @"SELECT CategoryId, RestaurantId, Name, DisplayOrder, IsActive
              FROM dbo.Categories WHERE CategoryId = @categoryId AND RestaurantId = @restaurantId",
            new { categoryId, restaurantId });
    }

    public async Task<bool> NameExistsAsync(int restaurantId, string name, int? excludeCategoryId = null)
    {
        const string sql = @"
            SELECT COUNT(1) FROM dbo.Categories
            WHERE RestaurantId = @restaurantId AND Name = @name
              AND (@excludeCategoryId IS NULL OR CategoryId <> @excludeCategoryId);";

        using var db = _factory.CreateConnection();
        return await db.ExecuteScalarAsync<int>(sql, new { restaurantId, name, excludeCategoryId }) > 0;
    }

    public async Task<int> CreateAsync(int restaurantId, string name)
    {
        // DisplayOrder = current maximum + 1, so the new category goes last
        const string sql = @"
            INSERT INTO dbo.Categories (RestaurantId, Name, DisplayOrder)
            OUTPUT INSERTED.CategoryId
            SELECT @restaurantId, @name, ISNULL(MAX(DisplayOrder), 0) + 1
            FROM dbo.Categories WHERE RestaurantId = @restaurantId;";

        using var db = _factory.CreateConnection();
        return await db.ExecuteScalarAsync<int>(sql, new { restaurantId, name });
    }

    public async Task<bool> RenameAsync(int categoryId, int restaurantId, string name)
    {
        using var db = _factory.CreateConnection();
        return await db.ExecuteAsync(
            "UPDATE dbo.Categories SET Name = @name WHERE CategoryId = @categoryId AND RestaurantId = @restaurantId",
            new { categoryId, restaurantId, name }) > 0;
    }

    public async Task<bool> SetActiveAsync(int categoryId, int restaurantId, bool isActive)
    {
        using var db = _factory.CreateConnection();
        return await db.ExecuteAsync(
            "UPDATE dbo.Categories SET IsActive = @isActive WHERE CategoryId = @categoryId AND RestaurantId = @restaurantId",
            new { categoryId, restaurantId, isActive }) > 0;
    }

    public async Task<bool> MoveAsync(int categoryId, int restaurantId, bool up)
    {
        using var db = _factory.CreateConnection();
        db.Open();
        using var tx = db.BeginTransaction();   // if anything throws, the transaction rolls back on dispose

        var current = await db.QuerySingleOrDefaultAsync<Category>(
            @"SELECT CategoryId, RestaurantId, Name, DisplayOrder, IsActive
              FROM dbo.Categories WHERE CategoryId = @categoryId AND RestaurantId = @restaurantId",
            new { categoryId, restaurantId }, tx);
        if (current is null) return false;

        var neighbourSql = up
            ? @"SELECT TOP 1 CategoryId, RestaurantId, Name, DisplayOrder, IsActive FROM dbo.Categories
                WHERE RestaurantId = @restaurantId AND DisplayOrder < @order ORDER BY DisplayOrder DESC"
            : @"SELECT TOP 1 CategoryId, RestaurantId, Name, DisplayOrder, IsActive FROM dbo.Categories
                WHERE RestaurantId = @restaurantId AND DisplayOrder > @order ORDER BY DisplayOrder ASC";

        var neighbour = await db.QuerySingleOrDefaultAsync<Category>(
            neighbourSql, new { restaurantId, order = current.DisplayOrder }, tx);

        if (neighbour is not null)
        {
            // Swap the two DisplayOrder values (one statement run once per object in the array)
            await db.ExecuteAsync(
                "UPDATE dbo.Categories SET DisplayOrder = @order WHERE CategoryId = @id",
                new[]
                {
                    new { id = current.CategoryId,   order = neighbour.DisplayOrder },
                    new { id = neighbour.CategoryId, order = current.DisplayOrder }
                }, tx);
        }

        tx.Commit();
        return true;   // already first/last is not an error: nothing to move
    }

    public async Task<DeleteCategoryResult> DeleteAsync(int categoryId, int restaurantId)
    {
        // Deletes only if it belongs to this restaurant AND has no items
        const string sql = @"
            DELETE FROM dbo.Categories
            WHERE CategoryId = @categoryId AND RestaurantId = @restaurantId
              AND NOT EXISTS (SELECT 1 FROM dbo.MenuItems WHERE CategoryId = @categoryId);";

        using var db = _factory.CreateConnection();
        var rows = await db.ExecuteAsync(sql, new { categoryId, restaurantId });
        if (rows > 0) return DeleteCategoryResult.Deleted;

        // Nothing deleted: either it does not exist for this restaurant, or it still has items
        var exists = await GetAsync(categoryId, restaurantId);
        return exists is null ? DeleteCategoryResult.NotFound : DeleteCategoryResult.HasItems;
    }
}