using MenuMint.Models;

namespace MenuMint.Interfaces;

public interface ICurrentRestaurantService
{
    /// <summary>The logged-in user's id, or null if nobody is logged in.</summary>
    string? UserId { get; }

    /// <summary>The restaurant owned by the logged-in user. Null if not logged in or no restaurant exists yet.</summary>
    Task<Restaurant?> GetAsync();

    Task<int?> GetIdAsync();

    /// <summary>Forget the cached restaurant (call after changing it within the same request).</summary>
    void Invalidate();
}