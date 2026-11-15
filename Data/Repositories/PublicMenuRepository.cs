using Dapper;
using MenuMint.Interfaces;
using MenuMint.Interfaces;
using MenuMint.Models;

namespace MenuMint.Data.Repositories;

public class PublicMenuRepository : IPublicMenuRepository
{
    private readonly IDbConnectionFactory _factory;

    public PublicMenuRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<PublicMenu?> GetBySlugAsync(string slug)
    {
        // Three result sets in ONE database round trip
        const string sql = @"
            SELECT RestaurantId, OwnerUserId, Name, Slug, Description, Address, Phone,
                   LogoPath, CoverPath, IsActive, CreatedAt, QrGeneratedAt
            FROM dbo.Restaurants
            WHERE Slug = @slug;

            SELECT c.CategoryId, c.RestaurantId, c.Name, c.DisplayOrder, c.IsActive
            FROM dbo.Categories c
            JOIN dbo.Restaurants r ON r.RestaurantId = c.RestaurantId
            WHERE r.Slug = @slug AND r.IsActive = 1 AND c.IsActive = 1
            ORDER BY c.DisplayOrder;

            SELECT i.MenuItemId, i.RestaurantId, i.CategoryId, i.Name, i.Description,
                   i.Price, i.Emoji, i.ImagePath, i.IsAvailable
            FROM dbo.MenuItems i
            JOIN dbo.Categories c ON c.CategoryId = i.CategoryId
            JOIN dbo.Restaurants r ON r.RestaurantId = i.RestaurantId
            WHERE r.Slug = @slug AND r.IsActive = 1 AND c.IsActive = 1
            ORDER BY i.IsAvailable DESC, i.Name;";

        using var db = _factory.CreateConnection();
        using var grid = await db.QueryMultipleAsync(sql, new { slug });

        // The result sets must be read in the same order as the SELECT statements
        var restaurant = await grid.ReadSingleOrDefaultAsync<Restaurant>();
        var categories = (await grid.ReadAsync<Category>()).ToList();
        var items = (await grid.ReadAsync<MenuItem>()).ToList();

        if (restaurant is null) return null;

        var itemsByCategory = items.GroupBy(i => i.CategoryId).ToDictionary(g => g.Key, g => g.ToList());

        return new PublicMenu
        {
            Restaurant = restaurant,
            Sections = categories
                .Where(c => itemsByCategory.ContainsKey(c.CategoryId))   // no empty sections on the public page
                .Select(c => new PublicMenuSection { Category = c, Items = itemsByCategory[c.CategoryId] })
                .ToList()
        };
    }
}