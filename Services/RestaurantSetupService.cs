using MenuMint.Interfaces;
using MenuMint.Models;
using Microsoft.Data.SqlClient;

namespace MenuMint.Services;

public class RestaurantSetupService : IRestaurantSetupService
{
    private const int MaxAttempts = 3;

    private readonly IRestaurantRepository _restaurants;
    private readonly ISlugService _slugs;
    private readonly ILogger<RestaurantSetupService> _logger;

    public RestaurantSetupService(IRestaurantRepository restaurants, ISlugService slugs,
        ILogger<RestaurantSetupService> logger)
    {
        _restaurants = restaurants;
        _slugs = slugs;
        _logger = logger;
    }

    public async Task<int> CreateForOwnerAsync(string ownerUserId, string restaurantName)
    {
        var name = restaurantName.Trim();

        for (var attempt = 1; ; attempt++)
        {
            var slug = await _slugs.GenerateUniqueSlugAsync(name);

            try
            {
                return await _restaurants.CreateAsync(new Restaurant
                {
                    OwnerUserId = ownerUserId,
                    Name = name,
                    Slug = slug,
                    Description = string.Empty,
                    Address = string.Empty,
                    Phone = string.Empty,
                    IsActive = true
                });
            }
            catch (SqlException ex) when (IsSlugConflict(ex) && attempt < MaxAttempts)
            {
                // The slug looked free, but someone inserted it a moment before us. Ask for a new one.
                _logger.LogWarning("Slug '{Slug}' was taken between the check and the insert (attempt {Attempt}). Retrying.", slug, attempt);
            }
        }
    }

    // 2627 / 2601 = unique constraint / unique index violation
    private static bool IsSlugConflict(SqlException ex) =>
        ex.Number is 2627 or 2601 &&
        ex.Message.Contains("UQ_Restaurants_Slug", StringComparison.OrdinalIgnoreCase);
}