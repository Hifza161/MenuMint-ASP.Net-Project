using MenuMint.Models;

namespace MenuMint.Interfaces;

public interface IMenuItemRepository
{
    /// <summary>All items of a restaurant with their category name, ordered by category then name.</summary>
    Task<List<MenuItemRow>> GetByRestaurantAsync(int restaurantId);

    /// <summary>The newest items first.</summary>
    Task<List<MenuItemRow>> GetRecentAsync(int restaurantId, int take);

    Task<MenuItem?> GetAsync(int menuItemId, int restaurantId);

    /// <summary>Returns the new id, or 0 if the category does not belong to this restaurant.</summary>
    Task<int> CreateAsync(MenuItem item);

    /// <summary>False if the item or the chosen category does not belong to this restaurant.</summary>
    Task<bool> UpdateAsync(MenuItem item);

    /// <summary>Flips available / sold out. Null if the item is not found for this restaurant.</summary>
    Task<ItemToggleResult?> ToggleAvailabilityAsync(int menuItemId, int restaurantId);

    /// <summary>Deletes the item and returns its name and image path, or null if not found.</summary>
    Task<DeletedItem?> DeleteAsync(int menuItemId, int restaurantId);

    Task<ItemCounts> GetCountsAsync(int restaurantId);
}