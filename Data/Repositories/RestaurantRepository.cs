using Dapper;
using MenuMint.Interfaces;
using MenuMint.Models;

namespace MenuMint.Data.Repositories;

public class RestaurantRepository : IRestaurantRepository
{
    private readonly IDbConnectionFactory _factory;

    public RestaurantRepository(IDbConnectionFactory factory) => _factory = factory;

    // A constant column list (no user input), shared by the SELECT queries
    private const string Columns =
        "RestaurantId, OwnerUserId, Name, Slug, Description, Address, Phone, LogoPath, CoverPath, IsActive, CreatedAt, QrGeneratedAt";

    public async Task<Restaurant?> GetByIdAsync(int restaurantId)
    {
        using var db = _factory.CreateConnection();
        return await db.QuerySingleOrDefaultAsync<Restaurant>(
            "SELECT " + Columns + " FROM dbo.Restaurants WHERE RestaurantId = @restaurantId",
            new { restaurantId });
    }

    public async Task<Restaurant?> GetByOwnerIdAsync(string ownerUserId)
    {
        using var db = _factory.CreateConnection();
        return await db.QuerySingleOrDefaultAsync<Restaurant>(
            "SELECT " + Columns + " FROM dbo.Restaurants WHERE OwnerUserId = @ownerUserId",
            new { ownerUserId });
    }

    public async Task<Restaurant?> GetBySlugAsync(string slug)
    {
        using var db = _factory.CreateConnection();
        return await db.QuerySingleOrDefaultAsync<Restaurant>(
            "SELECT " + Columns + " FROM dbo.Restaurants WHERE Slug = @slug",
            new { slug });
    }

    public async Task<bool> SlugExistsAsync(string slug)
    {
        using var db = _factory.CreateConnection();
        var count = await db.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM dbo.Restaurants WHERE Slug = @slug",
            new { slug });
        return count > 0;
    }

    public async Task<int> CreateAsync(Restaurant restaurant)
    {
        const string sql = @"
            INSERT INTO dbo.Restaurants (OwnerUserId, Name, Slug, Description, Address, Phone, LogoPath, CoverPath, IsActive)
            OUTPUT INSERTED.RestaurantId
            VALUES (@OwnerUserId, @Name, @Slug, @Description, @Address, @Phone, @LogoPath, @CoverPath, @IsActive);";

        using var db = _factory.CreateConnection();
        // Dapper reads the @Parameters from the matching properties of the object
        return await db.ExecuteScalarAsync<int>(sql, restaurant);
    }

    public async Task<bool> UpdateProfileAsync(Restaurant restaurant)
    {
        const string sql = @"
            UPDATE dbo.Restaurants
            SET Name = @Name, Description = @Description, Address = @Address, Phone = @Phone,
                LogoPath = @LogoPath, CoverPath = @CoverPath
            WHERE RestaurantId = @RestaurantId;";

        using var db = _factory.CreateConnection();
        return await db.ExecuteAsync(sql, restaurant) > 0;
    }

    public async Task<bool> SetActiveAsync(int restaurantId, bool isActive)
    {
        using var db = _factory.CreateConnection();
        return await db.ExecuteAsync(
            "UPDATE dbo.Restaurants SET IsActive = @isActive WHERE RestaurantId = @restaurantId",
            new { restaurantId, isActive }) > 0;
    }

    public async Task<bool> MarkQrGeneratedAsync(int restaurantId)
    {
        // Only the first visit counts, so later visits do not overwrite the date
        using var db = _factory.CreateConnection();
        return await db.ExecuteAsync(
            "UPDATE dbo.Restaurants SET QrGeneratedAt = COALESCE(QrGeneratedAt, SYSUTCDATETIME()) WHERE RestaurantId = @restaurantId",
            new { restaurantId }) > 0;
    }
}