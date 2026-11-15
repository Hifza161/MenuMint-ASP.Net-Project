using MenuMint.Models;

namespace MenuMint.Interfaces;

public interface IRestaurantRepository
{
    Task<Restaurant?> GetByIdAsync(int restaurantId);
    Task<Restaurant?> GetByOwnerIdAsync(string ownerUserId);
    Task<Restaurant?> GetBySlugAsync(string slug);
    Task<bool> SlugExistsAsync(string slug);

    /// <summary>Inserts the restaurant and returns its new id.</summary>
    Task<int> CreateAsync(Restaurant restaurant);

    /// <summary>Updates name, description, address, phone, logo and cover. The slug never changes.</summary>
    Task<bool> UpdateProfileAsync(Restaurant restaurant);

    Task<bool> SetActiveAsync(int restaurantId, bool isActive);
    Task<bool> MarkQrGeneratedAsync(int restaurantId);
}