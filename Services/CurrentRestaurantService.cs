using MenuMint.Interfaces;
using MenuMint.Models;
using System.Security.Claims;

namespace MenuMint.Services;

public class CurrentRestaurantService : ICurrentRestaurantService
{
    private readonly IHttpContextAccessor _http;
    private readonly IRestaurantRepository _restaurants;

    private Restaurant? _cached;
    private bool _loaded;

    public CurrentRestaurantService(IHttpContextAccessor http, IRestaurantRepository restaurants)
    {
        _http = http;
        _restaurants = restaurants;
    }

    public string? UserId => _http.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

    public async Task<Restaurant?> GetAsync()
    {
        if (_loaded) return _cached;   // scoped service: one database lookup per request, however often it is called

        var userId = UserId;
        _cached = string.IsNullOrEmpty(userId) ? null : await _restaurants.GetByOwnerIdAsync(userId);
        _loaded = true;
        return _cached;
    }

    public async Task<int?> GetIdAsync() => (await GetAsync())?.RestaurantId;

    public void Invalidate()
    {
        _cached = null;
        _loaded = false;
    }
}